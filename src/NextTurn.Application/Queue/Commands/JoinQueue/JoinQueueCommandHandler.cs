using MediatR;
using Microsoft.Extensions.Logging;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.Application.Queue.Commands.JoinQueue;

/// <summary>
/// Handles the JoinQueueCommand — orchestrates the full queue-join flow.
///
/// Input validation (non-empty GUIDs) runs automatically via ValidationBehavior before
/// this handler is invoked.
///
/// 7-step flow:
///   1. Fetch queue by ID — DomainException if not found
///   2. Check for existing active entry — ConflictDomainException if already joined
///   3. Count active entries → CanAcceptEntry — QueueFullDomainException if full
///   4. Assign next sequential ticket number
///   5. Create QueueEntry domain entity
///   6. Persist entry + save unit of work
///   7. Compute position and ETA, return JoinQueueResult
///
/// Position is computed inline (activeCount + 1) because the handler already holds
/// activeCount from step 3 — no extra round-trip needed.
/// IQueueStateService is not called in this handler; it is the seam for a future
/// GET /queues/{id}/my-status polling endpoint that will need cached position reads.
/// </summary>
public class JoinQueueCommandHandler : IRequestHandler<JoinQueueCommand, JoinQueueResult>
{
    private readonly IQueueRepository    _queueRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<JoinQueueCommandHandler> _logger;

    public JoinQueueCommandHandler(
        IQueueRepository     queueRepository,
        IApplicationDbContext context,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<JoinQueueCommandHandler> logger)
    {
        _queueRepository = queueRepository;
        _context         = context;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<JoinQueueResult> Handle(
        JoinQueueCommand  command,
        CancellationToken cancellationToken)
    {
        // Step 1 — load the queue aggregate
        var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        // Step 2 — guard against duplicate joins
        // A user may not hold more than one active (Waiting or Serving) entry per queue.
        bool alreadyJoined = await _queueRepository.HasActiveEntryAsync(
            command.QueueId, command.UserId, cancellationToken);
        if (alreadyJoined)
            throw new ConflictDomainException("Already in this queue.");

        // Step 3 — enforce capacity limit
        // CanAcceptEntry is a pure domain method: activeCount < MaxCapacity.
        int activeCount = await _queueRepository.GetActiveEntryCountAsync(
            command.QueueId, cancellationToken);
        if (!queue.CanAcceptEntry(activeCount))
            throw new QueueFullDomainException();

        // Step 4 — obtain the next ticket number
        // Computed as MAX(TicketNumber) + 1 in the repository, or 1 if the queue is empty.
        int ticketNumber = await _queueRepository.GetNextTicketNumberAsync(
            command.QueueId, cancellationToken);

        // Step 5 — create the domain entity
        var entry = QueueEntry.Create(command.QueueId, command.UserId, ticketNumber);

        // Pre-compute for both response and in-app notification.
        int position = activeCount + 1;
        int estimatedWaitSecs = queue.CalculateEtaSeconds(position);

        // Step 6 — persist
        await _queueRepository.AddEntryAsync(entry, cancellationToken);
        _context.UserInAppNotifications.Add(
            UserInAppNotification.QueueJoined(
                organisationId: queue.OrganisationId,
                userId: command.UserId,
                queueId: queue.Id,
                queueEntryId: entry.Id,
                queueName: queue.Name,
                ticketNumber: ticketNumber,
                positionInQueue: position));
        await _context.SaveChangesAsync(cancellationToken);

        await TrySendJoinNotificationAsync(
            command.UserId,
            queue.Name,
            ticketNumber,
            position,
            estimatedWaitSecs,
            cancellationToken);

        return new JoinQueueResult(
            TicketNumber:         ticketNumber,
            PositionInQueue:      position,
            EstimatedWaitSeconds: estimatedWaitSecs);
    }

    private async Task TrySendJoinNotificationAsync(
        Guid userId,
        string queueName,
        int ticketNumber,
        int position,
        int estimatedWaitSeconds,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
            return;

        try
        {
            await _emailService.SendQueueJoinedEmailAsync(
                toEmail: user.Email.Value,
                queueName: queueName,
                ticketNumber: ticketNumber,
                positionInQueue: position,
                estimatedWaitSeconds: estimatedWaitSeconds,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Queue join notification failed for user {UserId} in queue '{QueueName}'.",
                userId,
                queueName);
        }
    }
}

using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.NotifyApproachingTurn;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.LeaveQueue;

/// <summary>
/// Handles the LeaveQueueCommand — orchestrates the full queue-leave (cancel entry) flow.
///
/// Input validation (non-empty GUIDs) runs automatically via ValidationBehavior before
/// this handler is invoked.
///
/// 4-step flow:
///   1. Cancel the user's active entry in the queue — DomainException if not found
///   2. Persist the mutation
///   4. Return (implicit success)
///
/// The repository operation is scoped by user and queue ID, ensuring the user can
/// only cancel their own entry and cannot access other users' entries.
/// </summary>
public class LeaveQueueCommandHandler : IRequestHandler<LeaveQueueCommand, Unit>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public LeaveQueueCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context,
        ISender sender)
    {
        _queueRepository = queueRepository;
        _context = context;
        _sender = sender;
    }

    public async Task<Unit> Handle(
        LeaveQueueCommand command,
        CancellationToken cancellationToken)
    {
        // Step 1 — cancel the user's active entry (Waiting or Serving) in this queue
        bool cancelled = await _queueRepository.CancelEntryAsync(
            command.QueueId, command.UserId, cancellationToken);

        if (!cancelled)
            throw new DomainException("You are not in this queue.");

        // Step 2 — persist the state transition
        await _context.SaveChangesAsync(cancellationToken);
        await _sender.Send(new NotifyApproachingTurnCommand(command.QueueId), cancellationToken);

        return Unit.Value;
    }
}

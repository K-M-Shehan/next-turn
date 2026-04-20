using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Queue.Commands.NotifyApproachingTurn;

public sealed class NotifyApproachingTurnCommandHandler
    : IRequestHandler<NotifyApproachingTurnCommand, NotifyApproachingTurnResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public NotifyApproachingTurnCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<NotifyApproachingTurnResult> Handle(
        NotifyApproachingTurnCommand request,
        CancellationToken cancellationToken)
    {
        var queue = await _context.Queues
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == request.QueueId, cancellationToken);

        if (queue is null)
            throw new DomainException("Queue not found.");

        var organisation = await _context.Organisations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == queue.OrganisationId, cancellationToken);

        if (organisation is null)
            throw new DomainException("Organisation not found.");

        var threshold = organisation.QueueNotificationThreshold;

        var waitingEntries = await _context.QueueEntries
            .AsNoTracking()
            .Where(e => e.QueueId == request.QueueId && e.Status == Domain.Queue.Enums.QueueEntryStatus.Waiting)
            .OrderBy(e => e.TicketNumber)
            .ThenBy(e => e.JoinedAt)
            .Select(e => new CandidateEntry(e.Id, e.UserId, e.TicketNumber))
            .ToListAsync(cancellationToken);

        if (waitingEntries.Count == 0)
            return new NotifyApproachingTurnResult(0, 0);

        var thresholdEntries = waitingEntries
            .Select((entry, index) => new CandidateWithPosition(entry, index + 1))
            .Where(x => x.PositionInQueue <= threshold)
            .ToList();

        if (thresholdEntries.Count == 0)
            return new NotifyApproachingTurnResult(0, 0);

        var candidateEntryIds = thresholdEntries.Select(x => x.Entry.Id).ToList();

        var alreadySentIds = await _context.QueueTurnNotificationAuditLogs
            .AsNoTracking()
            .Where(l => candidateEntryIds.Contains(l.QueueEntryId) && l.DeliveryStatus == "Sent")
            .Select(l => l.QueueEntryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int sentCount = 0;
        int failedCount = 0;
        int inAppCreatedCount = 0;

        foreach (var candidate in thresholdEntries)
        {
            if (alreadySentIds.Contains(candidate.Entry.Id))
                continue;

            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Id == candidate.Entry.UserId,
                    cancellationToken);

            if (user is null || !user.IsActive)
                continue;

            var hasUnreadApproaching = await _context.UserInAppNotifications
                .AsNoTracking()
                .AnyAsync(
                    n => n.UserId == user.Id
                      && n.NotificationType == "QueueTurnApproaching"
                      && n.QueueEntryId == candidate.Entry.Id
                      && !n.IsRead,
                    cancellationToken);

            if (!hasUnreadApproaching)
            {
                _context.UserInAppNotifications.Add(
                    UserInAppNotification.QueueTurnApproaching(
                        organisationId: queue.OrganisationId,
                        userId: user.Id,
                        queueId: queue.Id,
                        queueEntryId: candidate.Entry.Id,
                        queueName: queue.Name,
                        ticketNumber: candidate.Entry.TicketNumber,
                        positionInQueue: candidate.PositionInQueue));

                inAppCreatedCount++;
            }

            if (!user.QueueTurnApproachingNotificationsEnabled)
                continue;

            try
            {
                await _emailService.SendQueueTurnApproachingEmailAsync(
                    toEmail: user.Email.Value,
                    queueName: queue.Name,
                    ticketNumber: candidate.Entry.TicketNumber,
                    positionInQueue: candidate.PositionInQueue,
                    cancellationToken: cancellationToken);

                _context.QueueTurnNotificationAuditLogs.Add(
                    Domain.Queue.Entities.QueueTurnNotificationAuditLog.Sent(
                        organisationId: queue.OrganisationId,
                        queueId: queue.Id,
                        queueEntryId: candidate.Entry.Id,
                        userId: user.Id,
                        positionInQueue: candidate.PositionInQueue,
                        threshold: threshold));

                sentCount++;
            }
            catch (Exception ex)
            {
                _context.QueueTurnNotificationAuditLogs.Add(
                    Domain.Queue.Entities.QueueTurnNotificationAuditLog.Failed(
                        organisationId: queue.OrganisationId,
                        queueId: queue.Id,
                        queueEntryId: candidate.Entry.Id,
                        userId: candidate.Entry.UserId,
                        positionInQueue: candidate.PositionInQueue,
                        threshold: threshold,
                        errorMessage: ex.Message));

                failedCount++;
            }
        }

        if (sentCount > 0 || failedCount > 0 || inAppCreatedCount > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return new NotifyApproachingTurnResult(sentCount, failedCount);
    }

    private sealed record CandidateEntry(Guid Id, Guid UserId, int TicketNumber);

    private sealed record CandidateWithPosition(CandidateEntry Entry, int PositionInQueue);
}

using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.NotifyApproachingTurn;
using NextTurn.Application.Queue.Commands;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Domain.Queue.Enums;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.Skip;

public sealed class SkipCommandHandler : IRequestHandler<SkipCommand, QueueEntryActionResult>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public SkipCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context,
        ISender sender)
    {
        _queueRepository = queueRepository;
        _context = context;
        _sender = sender;
    }

    public async Task<QueueEntryActionResult> Handle(
        SkipCommand command,
        CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        var entry = await _queueRepository.GetCurrentServingEntryAsync(command.QueueId, cancellationToken)
            ?? await _queueRepository.GetNextWaitingEntryAsync(command.QueueId, cancellationToken);

        if (entry is null)
            throw new DomainException("No active entry found in this queue.");

        if (entry.Status == QueueEntryStatus.Waiting)
            entry.StartServing();

        entry.MarkNoShow();

        _context.QueueActionAuditLogs.Add(
            QueueActionAuditLog.Create(
                organisationId: queue.OrganisationId,
                queueId: queue.Id,
                queueEntryId: entry.Id,
                performedByUserId: command.PerformedByUserId,
                actionType: QueueActionType.Skip,
                reason: command.Reason));

        await _context.SaveChangesAsync(cancellationToken);
        await _sender.Send(new NotifyApproachingTurnCommand(command.QueueId), cancellationToken);

        return new QueueEntryActionResult(entry.Id, entry.TicketNumber, entry.Status.ToString());
    }
}
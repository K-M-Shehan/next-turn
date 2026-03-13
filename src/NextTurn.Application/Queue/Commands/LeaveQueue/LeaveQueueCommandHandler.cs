using MediatR;
using NextTurn.Application.Common.Interfaces;
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
///   1. Fetch the user's active entry in the queue — DomainException if not found
///   2. Call Cancel() on the entry to transition it to Cancelled status
///   3. Persist the mutation
///   4. Return (implicit success)
///
/// The entry is loaded by user and queue ID, ensuring the user can only cancel
/// their own entry and cannot access other users' entries.
/// </summary>
public class LeaveQueueCommandHandler : IRequestHandler<LeaveQueueCommand>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;

    public LeaveQueueCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context)
    {
        _queueRepository = queueRepository;
        _context = context;
    }

    public async Task Handle(
        LeaveQueueCommand command,
        CancellationToken cancellationToken)
    {
        // Step 1 — load the user's active entry (Waiting or Serving) in this queue
        var entry = await _queueRepository.GetUserActiveEntryAsync(
            command.QueueId, command.UserId, cancellationToken);

        if (entry is null)
            throw new DomainException("You are not in this queue.");

        // Step 2 — cancel the entry
        // The Cancel() method enforces the invariant that only Waiting or Serving entries
        // may be cancelled — it will raise InvalidOperationException if violated.
        entry.Cancel();

        // Step 3 — persist the state transition
        await _context.SaveChangesAsync(cancellationToken);
    }
}

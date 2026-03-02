using NextTurn.Domain.Queue.Enums;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry  = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.Domain.Queue.Repositories;

/// <summary>
/// Persistence contract for the Queue aggregate and its QueueEntry children.
/// Implemented in NextTurn.Infrastructure; consumed by Application command handlers.
///
/// Design notes:
///   - Aggregate queries (GetByIdAsync) load the Queue root only — QueueEntry rows
///     are accessed via the scalar methods below to keep queries lean.
///   - "Active" entries are those with Status IN (Waiting, Serving).
///     This definition is owned by the repository, not the aggregate, because it
///     requires a DB query.
///
/// Note: Type aliases are used because the class name "Queue" would otherwise conflict
/// with the enclosing "NextTurn.Domain.Queue" namespace — the same technique used
/// in IOrganisationRepository (OrganisationEntity alias).
/// </summary>
public interface IQueueRepository
{
    /// <summary>
    /// Returns the queue with the given ID, or <c>null</c> if it does not exist.
    /// </summary>
    Task<QueueEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the number of entries currently in an active state
    /// (<see cref="QueueEntryStatus.Waiting"/> or <see cref="QueueEntryStatus.Serving"/>)
    /// for the specified queue.
    /// Used by the handler to pass into <see cref="Queue.CanAcceptEntry"/>.
    /// </summary>
    Task<int> GetActiveEntryCountAsync(Guid queueId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the next sequential ticket number for the given queue.
    /// Computed as MAX(TicketNumber) + 1 across all entries for that queue,
    /// or 1 if the queue has no entries yet.
    /// </summary>
    Task<int> GetNextTicketNumberAsync(Guid queueId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new queue entry.
    /// The caller is responsible for committing the unit of work (SaveChangesAsync).
    /// </summary>
    Task AddEntryAsync(QueueEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Returns <c>true</c> if the user already has an active entry
    /// (<see cref="QueueEntryStatus.Waiting"/> or <see cref="QueueEntryStatus.Serving"/>)
    /// in the specified queue.
    /// Used to enforce the "no duplicate join" constraint.
    /// </summary>
    Task<bool> HasActiveEntryAsync(Guid queueId, Guid userId, CancellationToken cancellationToken);
}

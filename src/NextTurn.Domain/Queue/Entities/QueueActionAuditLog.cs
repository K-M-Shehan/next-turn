using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Domain.Queue.Entities;

/// <summary>
/// Immutable audit row for staff queue actions.
/// </summary>
public sealed class QueueActionAuditLog
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid QueueId { get; private set; }
    public Guid QueueEntryId { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public QueueActionType ActionType { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public string? Reason { get; private set; }

    // Required by EF Core.
    private QueueActionAuditLog() { }

    private QueueActionAuditLog(
        Guid id,
        Guid organisationId,
        Guid queueId,
        Guid queueEntryId,
        Guid performedByUserId,
        QueueActionType actionType,
        DateTimeOffset createdAt,
        string? reason)
    {
        Id = id;
        OrganisationId = organisationId;
        QueueId = queueId;
        QueueEntryId = queueEntryId;
        PerformedByUserId = performedByUserId;
        ActionType = actionType;
        CreatedAt = createdAt;
        Reason = reason;
    }

    public static QueueActionAuditLog Create(
        Guid organisationId,
        Guid queueId,
        Guid queueEntryId,
        Guid performedByUserId,
        QueueActionType actionType,
        string? reason)
    {
        return new QueueActionAuditLog(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            queueId: queueId,
            queueEntryId: queueEntryId,
            performedByUserId: performedByUserId,
            actionType: actionType,
            createdAt: DateTimeOffset.UtcNow,
            reason: string.IsNullOrWhiteSpace(reason) ? null : reason.Trim());
    }
}
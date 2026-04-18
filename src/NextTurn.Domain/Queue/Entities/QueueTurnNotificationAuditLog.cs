namespace NextTurn.Domain.Queue.Entities;

/// <summary>
/// Immutable audit record for queue turn-approaching notification delivery attempts.
/// </summary>
public sealed class QueueTurnNotificationAuditLog
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid QueueId { get; private set; }
    public Guid QueueEntryId { get; private set; }
    public Guid UserId { get; private set; }
    public int PositionInQueue { get; private set; }
    public int Threshold { get; private set; }
    public string DeliveryStatus { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private QueueTurnNotificationAuditLog()
    {
        DeliveryStatus = default!;
    }

    private QueueTurnNotificationAuditLog(
        Guid id,
        Guid organisationId,
        Guid queueId,
        Guid queueEntryId,
        Guid userId,
        int positionInQueue,
        int threshold,
        string deliveryStatus,
        string? errorMessage,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganisationId = organisationId;
        QueueId = queueId;
        QueueEntryId = queueEntryId;
        UserId = userId;
        PositionInQueue = positionInQueue;
        Threshold = threshold;
        DeliveryStatus = deliveryStatus;
        ErrorMessage = errorMessage;
        CreatedAt = createdAt;
    }

    public static QueueTurnNotificationAuditLog Sent(
        Guid organisationId,
        Guid queueId,
        Guid queueEntryId,
        Guid userId,
        int positionInQueue,
        int threshold)
    {
        return new QueueTurnNotificationAuditLog(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            queueId: queueId,
            queueEntryId: queueEntryId,
            userId: userId,
            positionInQueue: positionInQueue,
            threshold: threshold,
            deliveryStatus: "Sent",
            errorMessage: null,
            createdAt: DateTimeOffset.UtcNow);
    }

    public static QueueTurnNotificationAuditLog Failed(
        Guid organisationId,
        Guid queueId,
        Guid queueEntryId,
        Guid userId,
        int positionInQueue,
        int threshold,
        string errorMessage)
    {
        var normalizedError = string.IsNullOrWhiteSpace(errorMessage)
            ? "Notification delivery failed."
            : errorMessage.Trim();

        return new QueueTurnNotificationAuditLog(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            queueId: queueId,
            queueEntryId: queueEntryId,
            userId: userId,
            positionInQueue: positionInQueue,
            threshold: threshold,
            deliveryStatus: "Failed",
            errorMessage: normalizedError.Length > 500 ? normalizedError[..500] : normalizedError,
            createdAt: DateTimeOffset.UtcNow);
    }
}

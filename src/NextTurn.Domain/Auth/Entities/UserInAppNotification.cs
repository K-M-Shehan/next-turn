namespace NextTurn.Domain.Auth.Entities;

public sealed class UserInAppNotification
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ReadAt { get; private set; }
    public Guid? QueueId { get; private set; }
    public Guid? QueueEntryId { get; private set; }

    private UserInAppNotification()
    {
        NotificationType = default!;
        Title = default!;
        Message = default!;
    }

    private UserInAppNotification(
        Guid id,
        Guid organisationId,
        Guid userId,
        string notificationType,
        string title,
        string message,
        bool isRead,
        DateTimeOffset createdAt,
        DateTimeOffset? readAt,
        Guid? queueId,
        Guid? queueEntryId)
    {
        Id = id;
        OrganisationId = organisationId;
        UserId = userId;
        NotificationType = notificationType;
        Title = title;
        Message = message;
        IsRead = isRead;
        CreatedAt = createdAt;
        ReadAt = readAt;
        QueueId = queueId;
        QueueEntryId = queueEntryId;
    }

    public static UserInAppNotification QueueJoined(
        Guid organisationId,
        Guid userId,
        Guid queueId,
        Guid queueEntryId,
        string queueName,
        int ticketNumber,
        int positionInQueue)
    {
        return new UserInAppNotification(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            userId: userId,
            notificationType: "QueueJoined",
            title: "Joined queue",
            message: $"You joined '{queueName}' with ticket #{ticketNumber}. Current position: {positionInQueue}.",
            isRead: false,
            createdAt: DateTimeOffset.UtcNow,
            readAt: null,
            queueId: queueId,
            queueEntryId: queueEntryId);
    }

    public static UserInAppNotification QueueTurnApproaching(
        Guid organisationId,
        Guid userId,
        Guid queueId,
        Guid queueEntryId,
        string queueName,
        int ticketNumber,
        int positionInQueue)
    {
        return new UserInAppNotification(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            userId: userId,
            notificationType: "QueueTurnApproaching",
            title: "Turn approaching",
            message: $"Queue '{queueName}': your ticket #{ticketNumber} is now at position {positionInQueue}.",
            isRead: false,
            createdAt: DateTimeOffset.UtcNow,
            readAt: null,
            queueId: queueId,
            queueEntryId: queueEntryId);
    }

    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }
}

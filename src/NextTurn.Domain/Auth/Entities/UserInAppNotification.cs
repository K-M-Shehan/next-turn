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

    public static UserInAppNotification AppointmentBooked(
        Guid organisationId,
        Guid userId,
        DateTimeOffset slotStart,
        string serviceName,
        string officeName)
    {
        return new UserInAppNotification(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            userId: userId,
            notificationType: "AppointmentBooked",
            title: "Appointment booked",
            message: $"Your {serviceName} appointment at {officeName} is confirmed for {slotStart:MMM d, yyyy h:mm tt}.",
            isRead: false,
            createdAt: DateTimeOffset.UtcNow,
            readAt: null,
            queueId: null,
            queueEntryId: null);
    }

    public static UserInAppNotification AppointmentRescheduled(
        Guid organisationId,
        Guid userId,
        DateTimeOffset slotStart,
        string serviceName,
        string officeName)
    {
        return new UserInAppNotification(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            userId: userId,
            notificationType: "AppointmentRescheduled",
            title: "Appointment rescheduled",
            message: $"Your {serviceName} appointment at {officeName} was rescheduled to {slotStart:MMM d, yyyy h:mm tt}.",
            isRead: false,
            createdAt: DateTimeOffset.UtcNow,
            readAt: null,
            queueId: null,
            queueEntryId: null);
    }

    public static UserInAppNotification AppointmentCancelled(
        Guid organisationId,
        Guid userId,
        DateTimeOffset slotStart,
        string serviceName,
        string officeName)
    {
        return new UserInAppNotification(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            userId: userId,
            notificationType: "AppointmentCancelled",
            title: "Appointment cancelled",
            message: $"Your {serviceName} appointment at {officeName} scheduled for {slotStart:MMM d, yyyy h:mm tt} was cancelled.",
            isRead: false,
            createdAt: DateTimeOffset.UtcNow,
            readAt: null,
            queueId: null,
            queueEntryId: null);
    }

    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }
}

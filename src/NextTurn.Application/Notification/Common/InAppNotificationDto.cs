namespace NextTurn.Application.Notification.Common;

public sealed record InAppNotificationDto(
    Guid NotificationId,
    string NotificationType,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    Guid? QueueId,
    Guid? QueueEntryId);

using MediatR;

namespace NextTurn.Application.Notification.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest<Unit>;

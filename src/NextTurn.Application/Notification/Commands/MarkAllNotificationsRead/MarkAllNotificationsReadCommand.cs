using MediatR;

namespace NextTurn.Application.Notification.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Unit>;

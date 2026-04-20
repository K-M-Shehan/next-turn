using MediatR;
using NextTurn.Application.Notification.Common;

namespace NextTurn.Application.Notification.Queries.ListMyNotifications;

public sealed record ListMyNotificationsQuery(Guid UserId, int Take = 20)
    : IRequest<IReadOnlyList<InAppNotificationDto>>;

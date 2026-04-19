using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Notification.Common;

namespace NextTurn.Application.Notification.Queries.ListMyNotifications;

public sealed class ListMyNotificationsQueryHandler
    : IRequestHandler<ListMyNotificationsQuery, IReadOnlyList<InAppNotificationDto>>
{
    private readonly IApplicationDbContext _context;

    public ListMyNotificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InAppNotificationDto>> Handle(
        ListMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.UserInAppNotifications
            .AsNoTracking()
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.Take)
            .Select(n => new InAppNotificationDto(
                n.Id,
                n.NotificationType,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt,
                n.QueueId,
                n.QueueEntryId))
            .ToListAsync(cancellationToken);

        return items;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;

namespace NextTurn.Application.Notification.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public MarkAllNotificationsReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _context.UserInAppNotifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
            return Unit.Value;

        foreach (var notification in unread)
            notification.MarkAsRead();

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Notification.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public MarkNotificationReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.UserInAppNotifications
            // Scope by user + notification ID to keep authorization strict while
            // allowing global users to read notifications across org tenants.
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId && n.UserId == request.UserId,
                cancellationToken);

        if (notification is null)
            throw new DomainException("Notification not found.");

        notification.MarkAsRead();
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.UpdateQueueNotificationPreference;

public sealed class UpdateQueueNotificationPreferenceCommandHandler
    : IRequestHandler<UpdateQueueNotificationPreferenceCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UpdateQueueNotificationPreferenceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateQueueNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new DomainException("User not found.");

        user.SetQueueTurnApproachingNotificationsEnabled(request.QueueTurnApproachingNotificationsEnabled);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

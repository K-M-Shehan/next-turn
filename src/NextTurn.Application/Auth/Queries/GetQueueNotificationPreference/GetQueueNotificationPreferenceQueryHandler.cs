using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Queries.GetQueueNotificationPreference;

public sealed class GetQueueNotificationPreferenceQueryHandler
    : IRequestHandler<GetQueueNotificationPreferenceQuery, QueueNotificationPreferenceResult>
{
    private readonly IApplicationDbContext _context;

    public GetQueueNotificationPreferenceQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QueueNotificationPreferenceResult> Handle(
        GetQueueNotificationPreferenceQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new DomainException("User not found.");

        return new QueueNotificationPreferenceResult(user.QueueTurnApproachingNotificationsEnabled);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Queries.GetAppointmentNotificationPreferences;

public sealed class GetAppointmentNotificationPreferencesQueryHandler
    : IRequestHandler<GetAppointmentNotificationPreferencesQuery, AppointmentNotificationPreferencesResult>
{
    private readonly IApplicationDbContext _context;

    public GetAppointmentNotificationPreferencesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppointmentNotificationPreferencesResult> Handle(
        GetAppointmentNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new DomainException("User not found.");

        return new AppointmentNotificationPreferencesResult(
            user.AppointmentBookedNotificationsEnabled,
            user.AppointmentRescheduledNotificationsEnabled,
            user.AppointmentCancelledNotificationsEnabled);
    }
}

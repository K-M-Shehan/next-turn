using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.UpdateAppointmentNotificationPreferences;

public sealed class UpdateAppointmentNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateAppointmentNotificationPreferencesCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UpdateAppointmentNotificationPreferencesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(
        UpdateAppointmentNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new DomainException("User not found.");

        user.SetAppointmentNotificationPreferences(
            request.AppointmentBookedNotificationsEnabled,
            request.AppointmentRescheduledNotificationsEnabled,
            request.AppointmentCancelledNotificationsEnabled);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

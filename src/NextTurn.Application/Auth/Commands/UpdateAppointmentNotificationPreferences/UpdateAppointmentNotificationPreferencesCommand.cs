using MediatR;

namespace NextTurn.Application.Auth.Commands.UpdateAppointmentNotificationPreferences;

public sealed record UpdateAppointmentNotificationPreferencesCommand(
    Guid UserId,
    bool AppointmentBookedNotificationsEnabled,
    bool AppointmentRescheduledNotificationsEnabled,
    bool AppointmentCancelledNotificationsEnabled) : IRequest<Unit>;

using MediatR;

namespace NextTurn.Application.Auth.Queries.GetAppointmentNotificationPreferences;

public sealed record GetAppointmentNotificationPreferencesQuery(Guid UserId)
    : IRequest<AppointmentNotificationPreferencesResult>;

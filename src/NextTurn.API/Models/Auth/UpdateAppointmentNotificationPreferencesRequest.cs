namespace NextTurn.API.Models.Auth;

public sealed record UpdateAppointmentNotificationPreferencesRequest(
    bool AppointmentBookedNotificationsEnabled,
    bool AppointmentRescheduledNotificationsEnabled,
    bool AppointmentCancelledNotificationsEnabled);

namespace NextTurn.Application.Auth.Queries.GetAppointmentNotificationPreferences;

public sealed record AppointmentNotificationPreferencesResult(
    bool AppointmentBookedNotificationsEnabled,
    bool AppointmentRescheduledNotificationsEnabled,
    bool AppointmentCancelledNotificationsEnabled);

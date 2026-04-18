using MediatR;
using NextTurn.Application.Appointment.Notifications;

namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

/// <summary>
/// Stub notification handler for appointment reschedules.
/// Real outbound delivery (email/SMS) is deferred to the Notifications epic.
/// </summary>
public sealed class AppointmentRescheduledNotificationHandler
    : INotificationHandler<AppointmentRescheduledNotification>
{
    private readonly IAppointmentNotificationService _appointmentNotificationService;

    public AppointmentRescheduledNotificationHandler(
        IAppointmentNotificationService appointmentNotificationService)
    {
        _appointmentNotificationService = appointmentNotificationService;
    }

    public Task Handle(
        AppointmentRescheduledNotification notification,
        CancellationToken cancellationToken)
    {
        return _appointmentNotificationService.SendRescheduleUpdateAsync(
            notification.NewAppointmentId,
            cancellationToken);
    }
}

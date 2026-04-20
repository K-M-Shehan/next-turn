using MediatR;
using NextTurn.Application.Appointment.Notifications;

namespace NextTurn.Application.Appointment.Commands.CancelAppointment;

/// <summary>
/// Stub notification handler for appointment cancellation events.
/// </summary>
public sealed class AppointmentCancelledNotificationHandler
    : INotificationHandler<AppointmentCancelledNotification>
{
    private readonly IAppointmentNotificationService _appointmentNotificationService;

    public AppointmentCancelledNotificationHandler(
        IAppointmentNotificationService appointmentNotificationService)
    {
        _appointmentNotificationService = appointmentNotificationService;
    }

    public Task Handle(
        AppointmentCancelledNotification notification,
        CancellationToken cancellationToken)
    {
        return _appointmentNotificationService.SendCancellationUpdateAsync(
            notification.AppointmentId,
            cancellationToken);
    }
}

using MediatR;
using NextTurn.Application.Appointment.Notifications;

namespace NextTurn.Application.Appointment.Commands.BookAppointment;

public sealed class AppointmentBookedNotificationHandler
    : INotificationHandler<AppointmentBookedNotification>
{
    private readonly IAppointmentNotificationService _appointmentNotificationService;

    public AppointmentBookedNotificationHandler(IAppointmentNotificationService appointmentNotificationService)
    {
        _appointmentNotificationService = appointmentNotificationService;
    }

    public Task Handle(AppointmentBookedNotification notification, CancellationToken cancellationToken)
    {
        return _appointmentNotificationService.SendBookingConfirmationAsync(notification.AppointmentId, cancellationToken);
    }
}

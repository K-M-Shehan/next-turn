namespace NextTurn.Application.Appointment.Notifications;

public interface IAppointmentNotificationService
{
    Task SendBookingConfirmationAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task SendRescheduleUpdateAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task SendCancellationUpdateAsync(Guid appointmentId, CancellationToken cancellationToken);
}

using MediatR;
using Moq;
using NextTurn.Application.Appointment.Commands.BookAppointment;
using NextTurn.Application.Appointment.Commands.CancelAppointment;
using NextTurn.Application.Appointment.Commands.RescheduleAppointment;
using NextTurn.Application.Appointment.Notifications;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class AppointmentNotificationHandlersTests
{
    private readonly Mock<IAppointmentNotificationService> _notificationServiceMock = new();

    [Fact]
    public async Task AppointmentBookedNotificationHandler_DelegatesToService()
    {
        var appointmentId = Guid.NewGuid();
        var handler = new AppointmentBookedNotificationHandler(_notificationServiceMock.Object);

        await handler.Handle(new AppointmentBookedNotification(appointmentId), CancellationToken.None);

        _notificationServiceMock.Verify(s => s.SendBookingConfirmationAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppointmentRescheduledNotificationHandler_DelegatesToService()
    {
        var appointmentId = Guid.NewGuid();
        var handler = new AppointmentRescheduledNotificationHandler(_notificationServiceMock.Object);

        await handler.Handle(
            new AppointmentRescheduledNotification(
                Guid.NewGuid(),
                appointmentId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(30),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30)),
            CancellationToken.None);

        _notificationServiceMock.Verify(s => s.SendRescheduleUpdateAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppointmentCancelledNotificationHandler_DelegatesToService()
    {
        var appointmentId = Guid.NewGuid();
        var handler = new AppointmentCancelledNotificationHandler(_notificationServiceMock.Object);

        await handler.Handle(
            new AppointmentCancelledNotification(
                appointmentId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(30),
                false),
            CancellationToken.None);

        _notificationServiceMock.Verify(s => s.SendCancellationUpdateAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

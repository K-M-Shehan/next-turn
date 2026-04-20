using FluentAssertions;
using NextTurn.Domain.Appointment.Entities;

namespace NextTurn.UnitTests.Domain.Appointment;

public sealed class AppointmentNotificationAuditLogTests
{
    [Fact]
    public void Sent_CreatesSuccessfulAuditRecord()
    {
        var before = DateTimeOffset.UtcNow;

        var log = AppointmentNotificationAuditLog.Sent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Booked",
            "user@example.com",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            "City Office",
            "Passport Service");

        var after = DateTimeOffset.UtcNow;

        log.Id.Should().NotBeEmpty();
        log.DeliveryStatus.Should().Be("Sent");
        log.NotificationType.Should().Be("Booked");
        log.ErrorMessage.Should().BeNull();
        log.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Failed_NormalizesAndTruncatesErrorMessage()
    {
        var longMessage = " " + new string('x', 600) + " ";

        var log = AppointmentNotificationAuditLog.Failed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cancelled",
            "user@example.com",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
            "City Office",
            "Passport Service",
            longMessage);

        log.DeliveryStatus.Should().Be("Failed");
        log.NotificationType.Should().Be("Cancelled");
        log.ErrorMessage.Should().NotBeNull();
        log.ErrorMessage!.Length.Should().Be(500);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NextTurn.Application.Appointment.Notifications;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.UnitTests.Helpers;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class AppointmentNotificationServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<AppointmentNotificationService>> _loggerMock = new();

    [Fact]
    public async Task SendBookingConfirmationAsync_WhenEnabled_SendsEmailAndWritesSentAudit()
    {
        var setup = BuildSetup();
        var addedLogs = new List<AppointmentNotificationAuditLog>();
        var addedInAppNotifications = new List<UserInAppNotification>();

        var auditDbSetMock = AsyncQueryableHelper.BuildMockDbSet(Array.Empty<AppointmentNotificationAuditLog>());
        auditDbSetMock
            .Setup(s => s.Add(It.IsAny<AppointmentNotificationAuditLog>()))
            .Callback<AppointmentNotificationAuditLog>(log => addedLogs.Add(log));

        var inAppDbSetMock = AsyncQueryableHelper.BuildMockDbSet(Array.Empty<UserInAppNotification>());
        inAppDbSetMock
            .Setup(s => s.Add(It.IsAny<UserInAppNotification>()))
            .Callback<UserInAppNotification>(notification => addedInAppNotifications.Add(notification));

        _contextMock.Setup(c => c.Appointments).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Appointment }).Object);
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.User }).Object);
        _contextMock.Setup(c => c.AppointmentProfiles).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Profile }).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Organisation }).Object);
        _contextMock.Setup(c => c.AppointmentNotificationAuditLogs).Returns(auditDbSetMock.Object);
        _contextMock.Setup(c => c.UserInAppNotifications).Returns(inAppDbSetMock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new AppointmentNotificationService(_contextMock.Object, _emailServiceMock.Object, _loggerMock.Object);

        await service.SendBookingConfirmationAsync(setup.Appointment.Id, CancellationToken.None);

        _emailServiceMock.Verify(
            e => e.SendAppointmentStatusEmailAsync(
                setup.User.Email.Value,
                setup.User.Name,
                "Booked",
                setup.Appointment.SlotStart,
                setup.Appointment.SlotEnd,
                setup.Organisation.Name,
                setup.Profile.Name,
                It.IsAny<CancellationToken>()),
            Times.Once);

        addedLogs.Should().ContainSingle();
        addedLogs[0].DeliveryStatus.Should().Be("Sent");
        addedLogs[0].NotificationType.Should().Be("Booked");
        addedInAppNotifications.Should().ContainSingle();
        addedInAppNotifications[0].NotificationType.Should().Be("AppointmentBooked");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_WhenPreferenceDisabled_SkipsDeliveryAndAudit()
    {
        var setup = BuildSetup();
        setup.User.SetAppointmentNotificationPreferences(false, true, true);
        var addedInAppNotifications = new List<UserInAppNotification>();

        _contextMock.Setup(c => c.Appointments).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Appointment }).Object);
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.User }).Object);
        _contextMock.Setup(c => c.AppointmentProfiles).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Profile }).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Organisation }).Object);
        _contextMock.Setup(c => c.AppointmentNotificationAuditLogs)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<AppointmentNotificationAuditLog>()).Object);
        _contextMock.Setup(c => c.UserInAppNotifications)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<UserInAppNotification>()).Object);
        _contextMock
            .Setup(c => c.UserInAppNotifications.Add(It.IsAny<UserInAppNotification>()))
            .Callback<UserInAppNotification>(n => addedInAppNotifications.Add(n));
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new AppointmentNotificationService(_contextMock.Object, _emailServiceMock.Object, _loggerMock.Object);

        await service.SendBookingConfirmationAsync(setup.Appointment.Id, CancellationToken.None);

        _emailServiceMock.Verify(
            e => e.SendAppointmentStatusEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        addedInAppNotifications.Should().ContainSingle();
        addedInAppNotifications[0].NotificationType.Should().Be("AppointmentBooked");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendRescheduleUpdateAsync_WhenEmailFails_WritesFailedAudit()
    {
        var setup = BuildSetup();
        var addedLogs = new List<AppointmentNotificationAuditLog>();
        var addedInAppNotifications = new List<UserInAppNotification>();

        var auditDbSetMock = AsyncQueryableHelper.BuildMockDbSet(Array.Empty<AppointmentNotificationAuditLog>());
        auditDbSetMock
            .Setup(s => s.Add(It.IsAny<AppointmentNotificationAuditLog>()))
            .Callback<AppointmentNotificationAuditLog>(log => addedLogs.Add(log));

        var inAppDbSetMock = AsyncQueryableHelper.BuildMockDbSet(Array.Empty<UserInAppNotification>());
        inAppDbSetMock
            .Setup(s => s.Add(It.IsAny<UserInAppNotification>()))
            .Callback<UserInAppNotification>(notification => addedInAppNotifications.Add(notification));

        _contextMock.Setup(c => c.Appointments).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Appointment }).Object);
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.User }).Object);
        _contextMock.Setup(c => c.AppointmentProfiles).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Profile }).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { setup.Organisation }).Object);
        _contextMock.Setup(c => c.AppointmentNotificationAuditLogs).Returns(auditDbSetMock.Object);
        _contextMock.Setup(c => c.UserInAppNotifications).Returns(inAppDbSetMock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _emailServiceMock
            .Setup(e => e.SendAppointmentStatusEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var service = new AppointmentNotificationService(_contextMock.Object, _emailServiceMock.Object, _loggerMock.Object);

        await service.SendRescheduleUpdateAsync(setup.Appointment.Id, CancellationToken.None);

        addedLogs.Should().ContainSingle();
        addedLogs[0].DeliveryStatus.Should().Be("Failed");
        addedLogs[0].NotificationType.Should().Be("Rescheduled");
        addedLogs[0].ErrorMessage.Should().Contain("smtp down");
        addedInAppNotifications.Should().ContainSingle();
        addedInAppNotifications[0].NotificationType.Should().Be("AppointmentRescheduled");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendCancellationUpdateAsync_WhenAppointmentMissing_DoesNothing()
    {
        _contextMock.Setup(c => c.Appointments)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<AppointmentEntity>()).Object);
        _contextMock.Setup(c => c.UserInAppNotifications)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<UserInAppNotification>()).Object);

        var service = new AppointmentNotificationService(_contextMock.Object, _emailServiceMock.Object, _loggerMock.Object);

        await service.SendCancellationUpdateAsync(Guid.NewGuid(), CancellationToken.None);

        _emailServiceMock.Verify(
            e => e.SendAppointmentStatusEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (AppointmentEntity Appointment, User User, AppointmentProfile Profile, OrganisationEntity Organisation) BuildSetup()
    {
        var organisation = OrganisationEntity.Create(
            "City Office",
            "city-office",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@city.gov"));

        var user = User.Create(organisation.Id, "Alice", new EmailAddress("alice@example.com"), null, "hash");
        var profile = AppointmentProfile.Create(organisation.Id, "Passport Service");

        var appointment = AppointmentEntity.Create(
            organisation.Id,
            profile.Id,
            user.Id,
            DateTimeOffset.UtcNow.AddDays(2),
            DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30));

        return (appointment, user, profile, organisation);
    }
}

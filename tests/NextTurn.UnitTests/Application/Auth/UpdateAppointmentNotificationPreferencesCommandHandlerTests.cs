using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.UpdateAppointmentNotificationPreferences;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.UnitTests.Helpers;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class UpdateAppointmentNotificationPreferencesCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesPreferencesAndSaves()
    {
        var user = User.Create(Guid.NewGuid(), "Member", new EmailAddress("member@example.com"), null, "hash");
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { user }).Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateAppointmentNotificationPreferencesCommandHandler(_contextMock.Object);

        var result = await handler.Handle(
            new UpdateAppointmentNotificationPreferencesCommand(user.Id, false, false, true),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.AppointmentBookedNotificationsEnabled.Should().BeFalse();
        user.AppointmentRescheduledNotificationsEnabled.Should().BeFalse();
        user.AppointmentCancelledNotificationsEnabled.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<User>()).Object);

        var handler = new UpdateAppointmentNotificationPreferencesCommandHandler(_contextMock.Object);
        var act = async () => await handler.Handle(
            new UpdateAppointmentNotificationPreferencesCommand(Guid.NewGuid(), true, true, true),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("User not found.");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

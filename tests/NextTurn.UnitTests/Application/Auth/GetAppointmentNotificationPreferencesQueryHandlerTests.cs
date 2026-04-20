using FluentAssertions;
using Moq;
using NextTurn.Application.Auth.Queries.GetAppointmentNotificationPreferences;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.UnitTests.Helpers;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class GetAppointmentNotificationPreferencesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsPreferences()
    {
        var user = User.Create(Guid.NewGuid(), "Member", new EmailAddress("member@example.com"), null, "hash");
        user.SetAppointmentNotificationPreferences(false, true, false);

        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { user }).Object);

        var handler = new GetAppointmentNotificationPreferencesQueryHandler(_contextMock.Object);
        var result = await handler.Handle(new GetAppointmentNotificationPreferencesQuery(user.Id), CancellationToken.None);

        result.AppointmentBookedNotificationsEnabled.Should().BeFalse();
        result.AppointmentRescheduledNotificationsEnabled.Should().BeTrue();
        result.AppointmentCancelledNotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<User>()).Object);

        var handler = new GetAppointmentNotificationPreferencesQueryHandler(_contextMock.Object);
        var act = async () => await handler.Handle(new GetAppointmentNotificationPreferencesQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("User not found.");
    }
}

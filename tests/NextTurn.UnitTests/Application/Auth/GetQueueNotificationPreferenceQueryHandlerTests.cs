using FluentAssertions;
using Moq;
using NextTurn.Application.Auth.Queries.GetQueueNotificationPreference;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.UnitTests.Helpers;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class GetQueueNotificationPreferenceQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsPreference()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "Member", new EmailAddress("member@example.com"), null, "hash");
        user.SetQueueTurnApproachingNotificationsEnabled(false);

        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { user }).Object);

        var handler = new GetQueueNotificationPreferenceQueryHandler(_contextMock.Object);
        var result = await handler.Handle(new GetQueueNotificationPreferenceQuery(user.Id), CancellationToken.None);

        result.QueueTurnApproachingNotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<User>()).Object);

        var handler = new GetQueueNotificationPreferenceQueryHandler(_contextMock.Object);
        var act = async () => await handler.Handle(new GetQueueNotificationPreferenceQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("User not found.");
    }
}

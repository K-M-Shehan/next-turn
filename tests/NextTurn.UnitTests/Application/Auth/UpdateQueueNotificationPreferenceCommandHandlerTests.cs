using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.UpdateQueueNotificationPreference;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.UnitTests.Helpers;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class UpdateQueueNotificationPreferenceCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenUserExists_UpdatesPreferenceAndSaves()
    {
        var tenantId = Guid.NewGuid();
        var user = User.Create(tenantId, "Member", new EmailAddress("member@example.com"), null, "hash");

        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { user }).Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateQueueNotificationPreferenceCommandHandler(_contextMock.Object);

        var result = await handler.Handle(
            new UpdateQueueNotificationPreferenceCommand(user.Id, false),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.QueueTurnApproachingNotificationsEnabled.Should().BeFalse();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<User>()).Object);

        var handler = new UpdateQueueNotificationPreferenceCommandHandler(_contextMock.Object);

        var act = async () => await handler.Handle(
            new UpdateQueueNotificationPreferenceCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("User not found.");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

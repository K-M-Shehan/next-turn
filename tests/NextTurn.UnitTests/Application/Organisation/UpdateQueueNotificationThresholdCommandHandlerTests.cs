using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Organisation.Commands.UpdateQueueNotificationThreshold;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.UnitTests.Helpers;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Organisation;

public sealed class UpdateQueueNotificationThresholdCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenOrganisationExists_UpdatesThresholdAndSaves()
    {
        var organisation = OrganisationEntity.Create(
            "City Council",
            "city-council",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@city.gov"));

        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { organisation }).Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateQueueNotificationThresholdCommandHandler(_contextMock.Object);

        var result = await handler.Handle(
            new UpdateQueueNotificationThresholdCommand(organisation.Id, 5),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        organisation.QueueNotificationThreshold.Should().Be(5);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrganisationMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Organisations)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<OrganisationEntity>()).Object);

        var handler = new UpdateQueueNotificationThresholdCommandHandler(_contextMock.Object);
        var act = async () => await handler.Handle(
            new UpdateQueueNotificationThresholdCommand(Guid.NewGuid(), 4),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Organisation not found.");
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

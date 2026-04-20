using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Organisation.Queries.GetQueueNotificationThreshold;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.UnitTests.Helpers;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Organisation;

public sealed class GetQueueNotificationThresholdQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenOrganisationExists_ReturnsThreshold()
    {
        var organisation = OrganisationEntity.Create(
            "Town Office",
            "town-office",
            new Address("2 Main", "Kandy", "20000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@town.gov"));
        organisation.SetQueueNotificationThreshold(6);

        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { organisation }).Object);

        var handler = new GetQueueNotificationThresholdQueryHandler(_contextMock.Object);
        var result = await handler.Handle(new GetQueueNotificationThresholdQuery(organisation.Id), CancellationToken.None);

        result.QueueNotificationThreshold.Should().Be(6);
    }

    [Fact]
    public async Task Handle_WhenOrganisationMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Organisations)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<OrganisationEntity>()).Object);

        var handler = new GetQueueNotificationThresholdQueryHandler(_contextMock.Object);
        var act = async () => await handler.Handle(new GetQueueNotificationThresholdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Organisation not found.");
    }
}

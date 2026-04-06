using FluentAssertions;
using Moq;
using NextTurn.Application.Service.Queries.ListServices;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.UnitTests.Application.Service;

public sealed class ListServicesQueryHandlerTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly ListServicesQueryHandler _handler;

    public ListServicesQueryHandlerTests()
    {
        _handler = new ListServicesQueryHandler(_serviceRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithAssignedOffices_MapsResultIncludingAssignmentIds()
    {
        var organisationId = Guid.NewGuid();
        var query = new ListServicesQuery(organisationId, true, 1, 20);

        var serviceA = ServiceEntity.Create(organisationId, "Passport", "SVC-01", "Passport service", 20, true);
        var serviceB = ServiceEntity.Create(organisationId, "License", "SVC-02", "License service", 15, true);

        _serviceRepositoryMock
            .Setup(r => r.ListAsync(organisationId, true, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ServiceEntity> { serviceA, serviceB }, 2));

        var office1 = Guid.NewGuid();
        var office2 = Guid.NewGuid();
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> assignmentMap = new Dictionary<Guid, IReadOnlyList<Guid>>
        {
            [serviceA.Id] = new List<Guid> { office1, office2 },
        };

        _serviceRepositoryMock
            .Setup(r => r.GetAssignedOfficeIdsByServiceIdsAsync(
                organisationId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignmentMap);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);

        var first = result.Items.Single(x => x.ServiceId == serviceA.Id);
        first.AssignedOfficeIds.Should().BeEquivalentTo(new[] { office1, office2 });

        var second = result.Items.Single(x => x.ServiceId == serviceB.Id);
        second.AssignedOfficeIds.Should().BeEmpty();
    }
}

using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Commands.AssignServiceOffices;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.UnitTests.Application.Service;

public sealed class AssignServiceOfficesCommandHandlerTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly AssignServiceOfficesCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ServiceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public AssignServiceOfficesCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new AssignServiceOfficesCommandHandler(_serviceRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingService_AssignsDistinctOfficesAndPersists()
    {
        var service = ServiceEntity.Create(OrganisationId, "Passport Renewal", "SVC-01", "Renewal", 20, true);
        var officeA = Guid.NewGuid();
        var officeB = Guid.NewGuid();

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var command = new AssignServiceOfficesCommand(OrganisationId, ServiceId, new[] { officeA, officeA, officeB });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);

        _serviceRepositoryMock.Verify(r => r.AssignOfficesAsync(
            OrganisationId,
            ServiceId,
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(officeA) && ids.Contains(officeB)),
            It.IsAny<CancellationToken>()), Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceNotFound_ThrowsDomainException()
    {
        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceEntity?)null);

        var command = new AssignServiceOfficesCommand(OrganisationId, ServiceId, new[] { Guid.NewGuid() });

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Service not found.");
    }
}

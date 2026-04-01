using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Commands.UpdateService;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.UnitTests.Application.Service;

public sealed class UpdateServiceCommandHandlerTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly UpdateServiceCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ServiceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public UpdateServiceCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new UpdateServiceCommandHandler(_serviceRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingService_UpdatesAndPersists()
    {
        var service = ServiceEntity.Create(OrganisationId, "Old Name", "SVC-01", "Old Description", 10, true);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var command = new UpdateServiceCommand(OrganisationId, ServiceId, "New Name", "Updated Description", 30);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("New Name");
        result.Description.Should().Be("Updated Description");
        result.EstimatedDurationMinutes.Should().Be(30);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceNotFound_ThrowsDomainException()
    {
        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceEntity?)null);

        var command = new UpdateServiceCommand(OrganisationId, ServiceId, "New Name", "Updated Description", 30);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Service not found.");
    }
}

using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Commands.DeactivateService;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.UnitTests.Application.Service;

public sealed class DeactivateServiceCommandHandlerTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly DeactivateServiceCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ServiceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public DeactivateServiceCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new DeactivateServiceCommandHandler(_serviceRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithActiveService_DeactivatesAndPersists()
    {
        var service = ServiceEntity.Create(OrganisationId, "Renewal", "SVC-01", "Desc", 15, true);

        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        await _handler.Handle(new DeactivateServiceCommand(OrganisationId, ServiceId), CancellationToken.None);

        service.IsActive.Should().BeFalse();
        service.DeactivatedAt.Should().NotBeNull();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceNotFound_ThrowsDomainException()
    {
        _serviceRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceEntity?)null);

        var act = async () => await _handler.Handle(new DeactivateServiceCommand(OrganisationId, ServiceId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Service not found.");
    }
}

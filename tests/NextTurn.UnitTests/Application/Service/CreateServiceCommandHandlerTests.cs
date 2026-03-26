using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Commands.CreateService;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.UnitTests.Application.Service;

public sealed class CreateServiceCommandHandlerTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly CreateServiceCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public CreateServiceCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreateServiceCommandHandler(_serviceRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithUniqueCode_CreatesServiceAndPersists()
    {
        _serviceRepositoryMock
            .Setup(r => r.ExistsByCodeAsync(OrganisationId, "SVC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateServiceCommand(
            OrganisationId,
            "Passport Renewal",
            "svc-01",
            "Renewal processing",
            20,
            true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ServiceId.Should().NotBeEmpty();
        result.Name.Should().Be("Passport Renewal");
        result.Code.Should().Be("SVC-01");

        _serviceRepositoryMock.Verify(
            r => r.AddAsync(It.Is<ServiceEntity>(s =>
                s.OrganisationId == OrganisationId &&
                s.Name == "Passport Renewal" &&
                s.Code == "SVC-01"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateCode_ThrowsConflictDomainException()
    {
        _serviceRepositoryMock
            .Setup(r => r.ExistsByCodeAsync(OrganisationId, "SVC-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateServiceCommand(
            OrganisationId,
            "Passport Renewal",
            "svc-01",
            "Renewal processing",
            20,
            true);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictDomainException>()
            .WithMessage("Service code already exists in this tenant.");
    }
}

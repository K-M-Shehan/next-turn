using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Office.Commands.DeactivateOffice;
using NextTurn.Domain.Common;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.UnitTests.Application.Office;

public sealed class DeactivateOfficeCommandHandlerTests
{
    private readonly Mock<IOfficeRepository> _officeRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly DeactivateOfficeCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OfficeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public DeactivateOfficeCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new DeactivateOfficeCommandHandler(_officeRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithActiveOffice_DeactivatesAndPersists()
    {
        var office = OfficeEntity.Create(OrganisationId, "Main", "Address", null, null, "9-5");

        _officeRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, OfficeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(office);

        await _handler.Handle(new DeactivateOfficeCommand(OrganisationId, OfficeId), CancellationToken.None);

        office.IsActive.Should().BeFalse();
        office.DeactivatedAt.Should().NotBeNull();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOfficeNotFound_ThrowsDomainException()
    {
        _officeRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, OfficeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity?)null);

        var act = async () => await _handler.Handle(new DeactivateOfficeCommand(OrganisationId, OfficeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Office not found.");
    }
}

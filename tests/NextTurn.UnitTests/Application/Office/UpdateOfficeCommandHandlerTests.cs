using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Office.Commands.UpdateOffice;
using NextTurn.Domain.Common;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.UnitTests.Application.Office;

public sealed class UpdateOfficeCommandHandlerTests
{
    private readonly Mock<IOfficeRepository> _officeRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly UpdateOfficeCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OfficeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public UpdateOfficeCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new UpdateOfficeCommandHandler(_officeRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOffice_UpdatesAndPersists()
    {
        var office = OfficeEntity.Create(OrganisationId, "Old", "Old Address", null, null, "9-5");

        _officeRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, OfficeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(office);

        var command = new UpdateOfficeCommand(
            OrganisationId,
            OfficeId,
            "Updated Office",
            "Updated Address",
            null,
            null,
            "{\"mon\":\"08:00-16:00\"}");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Updated Office");
        result.Address.Should().Be("Updated Address");
        result.OpeningHours.Should().Be("{\"mon\":\"08:00-16:00\"}");

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOfficeNotFound_ThrowsDomainException()
    {
        _officeRepositoryMock
            .Setup(r => r.GetByIdAsync(OrganisationId, OfficeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity?)null);

        var command = new UpdateOfficeCommand(
            OrganisationId,
            OfficeId,
            "Updated Office",
            "Updated Address",
            null,
            null,
            "{\"mon\":\"08:00-16:00\"}");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Office not found.");
    }
}

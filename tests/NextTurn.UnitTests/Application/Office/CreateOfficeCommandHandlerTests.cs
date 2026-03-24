using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Office.Commands.CreateOffice;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.UnitTests.Application.Office;

public sealed class CreateOfficeCommandHandlerTests
{
    private readonly Mock<IOfficeRepository> _officeRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly CreateOfficeCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public CreateOfficeCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreateOfficeCommandHandler(_officeRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesOfficeAndPersists()
    {
        var command = new CreateOfficeCommand(
            OrganisationId,
            "Main Branch",
            "123 Main Street",
            6.9271m,
            79.8612m,
            "{\"mon\":\"09:00-17:00\"}");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.OfficeId.Should().NotBeEmpty();
        result.Name.Should().Be("Main Branch");
        result.Address.Should().Be("123 Main Street");
        result.IsActive.Should().BeTrue();

        _officeRepositoryMock.Verify(
            r => r.AddAsync(It.Is<OfficeEntity>(o =>
                o.OrganisationId == OrganisationId &&
                o.Name == "Main Branch" &&
                o.Address == "123 Main Street"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

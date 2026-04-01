using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Commands.RemoveServiceOfficeAssignment;
using NextTurn.Domain.Service.Repositories;

namespace NextTurn.UnitTests.Application.Service;

public sealed class RemoveServiceOfficeAssignmentCommandHandlerTests
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly RemoveServiceOfficeAssignmentCommandHandler _handler;

    public RemoveServiceOfficeAssignmentCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RemoveServiceOfficeAssignmentCommandHandler(_serviceRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_RemovesAssignmentAndPersists()
    {
        var organisationId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var officeId = Guid.NewGuid();

        var command = new RemoveServiceOfficeAssignmentCommand(organisationId, serviceId, officeId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);

        _serviceRepositoryMock.Verify(r => r.RemoveOfficeAssignmentAsync(
            organisationId,
            serviceId,
            officeId,
            It.IsAny<CancellationToken>()), Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

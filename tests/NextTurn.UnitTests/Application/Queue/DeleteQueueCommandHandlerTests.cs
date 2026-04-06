using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.DeleteQueue;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class DeleteQueueCommandHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenQueueNotFound_Throws()
    {
        var orgId = Guid.NewGuid();
        var queueId = Guid.NewGuid();

        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var handler = new DeleteQueueCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);

        var act = async () => await handler.Handle(new DeleteQueueCommand(orgId, queueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenQueueBelongsToDifferentOrganisation_Throws()
    {
        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var queue = QueueEntity.Create(otherOrgId, "Counter A", 10, 120);

        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queue);

        var handler = new DeleteQueueCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);

        var act = async () => await handler.Handle(new DeleteQueueCommand(orgId, queueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenDeleteReturnsFalse_Throws()
    {
        var orgId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var queue = QueueEntity.Create(orgId, "Counter A", 10, 120);

        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queue);

        _queueRepositoryMock
            .Setup(r => r.DeleteQueueAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeleteQueueCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);

        var act = async () => await handler.Handle(new DeleteQueueCommand(orgId, queueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WithValidRequest_DeletesQueueAndSaves()
    {
        var orgId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var queue = QueueEntity.Create(orgId, "Counter A", 10, 120);

        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queue);

        _queueRepositoryMock
            .Setup(r => r.DeleteQueueAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteQueueCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);

        var result = await handler.Handle(new DeleteQueueCommand(orgId, queueId), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _queueRepositoryMock.Verify(r => r.DeleteQueueAsync(queueId, It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_WhenOrganisationIdIsEmpty_Fails()
    {
        var validator = new DeleteQueueCommandValidator();

        var result = validator.Validate(new DeleteQueueCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(DeleteQueueCommand.OrganisationId));
    }

    [Fact]
    public void Validator_WhenQueueIdIsEmpty_Fails()
    {
        var validator = new DeleteQueueCommandValidator();

        var result = validator.Validate(new DeleteQueueCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(DeleteQueueCommand.QueueId));
    }

    [Fact]
    public void Validator_WhenIdsArePresent_Passes()
    {
        var validator = new DeleteQueueCommandValidator();

        var result = validator.Validate(new DeleteQueueCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}

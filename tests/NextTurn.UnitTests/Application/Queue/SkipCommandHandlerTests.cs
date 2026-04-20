using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.NotifyApproachingTurn;
using NextTurn.Application.Queue.Commands.Skip;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Domain.Queue.Enums;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class SkipCommandHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<DbSet<QueueActionAuditLog>> _auditLogSetMock = new();
    private readonly Mock<ISender> _senderMock = new();

    private readonly SkipCommandHandler _handler;

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PerformedByUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public SkipCommandHandlerTests()
    {
        _contextMock
            .Setup(c => c.QueueActionAuditLogs)
            .Returns(_auditLogSetMock.Object);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue());

        _senderMock
            .Setup(s => s.Send(It.IsAny<NotifyApproachingTurnCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotifyApproachingTurnResult(0, 0));

        _handler = new SkipCommandHandler(_queueRepositoryMock.Object, _contextMock.Object, _senderMock.Object);
    }

    [Fact]
    public async Task Handle_WithServingEntry_MarksNoShow_AndWritesAuditWithReason()
    {
        var serving = QueueEntry.Create(QueueId, UserId, 2);
        serving.StartServing();

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serving);

        var result = await _handler.Handle(
            new SkipCommand(QueueId, PerformedByUserId, "Citizen did not arrive"),
            CancellationToken.None);

        result.Status.Should().Be("NoShow");

        _auditLogSetMock.Verify(
            s => s.Add(It.Is<QueueActionAuditLog>(a =>
                a.ActionType == QueueActionType.Skip &&
                a.QueueEntryId == serving.Id &&
                a.PerformedByUserId == PerformedByUserId &&
                a.Reason == "Citizen did not arrive")),
            Times.Once);

        _senderMock.Verify(s => s.Send(It.IsAny<NotifyApproachingTurnCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWaitingEntry_StartsServingThenMarksNoShow()
    {
        var waiting = QueueEntry.Create(QueueId, UserId, 1);

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        _queueRepositoryMock
            .Setup(r => r.GetNextWaitingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waiting);

        var result = await _handler.Handle(
            new SkipCommand(QueueId, PerformedByUserId, null),
            CancellationToken.None);

        result.Status.Should().Be("NoShow");
        waiting.Status.Should().Be(QueueEntryStatus.NoShow);
    }

    [Fact]
    public async Task Handle_WhenNoActiveEntry_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        _queueRepositoryMock
            .Setup(r => r.GetNextWaitingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        var act = async () => await _handler.Handle(new SkipCommand(QueueId, PerformedByUserId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("No active entry found in this queue.");
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(new SkipCommand(QueueId, PerformedByUserId, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Queue not found.");
    }

    private static QueueEntity BuildQueue() =>
        QueueEntity.Create(OrgId, "Main Queue", maxCapacity: 50, averageServiceTimeSeconds: 180);
}
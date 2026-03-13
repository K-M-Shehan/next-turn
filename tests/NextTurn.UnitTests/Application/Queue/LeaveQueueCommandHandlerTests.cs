using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.LeaveQueue;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for LeaveQueueCommandHandler.
///
/// All dependencies are Moq doubles — no database, no EF Core, no HTTP context.
/// Tests verify the handler's 4-step orchestration logic in isolation.
///
/// Key invariants exercised:
///   - User not in queue → DomainException "You are not in this queue."
///   - Happy path: entry is cancelled (GetUserActiveEntryAsync returns a valid entry)
///   - SaveChangesAsync called exactly once per successful leave
/// </summary>
public sealed class LeaveQueueCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly LeaveQueueCommandHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrgId = Guid.NewGuid();

    public LeaveQueueCommandHandlerTests()
    {
        // Default: user has an active entry in the queue
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueueEntry());

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new LeaveQueueCommandHandler(
            _queueRepositoryMock.Object,
            _contextMock.Object);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_Succeeds()
    {
        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_CallsGetUserActiveEntryAsyncWithCorrectParameters()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _queueRepositoryMock.Verify(
            r => r.GetUserActiveEntryAsync(QueueId, UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CancelsTheEntry()
    {
        var entry = BuildQueueEntry();
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        // Verify the entry's status is now Cancelled
        entry.Status.Should().Be(NextTurn.Domain.Queue.Enums.QueueEntryStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_CallsSaveChangesAsyncExactlyOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── User not in queue (step 1) ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotInQueue_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("You are not in this queue.");
    }

    [Fact]
    public async Task Handle_WhenUserNotInQueue_DoesNotCallSaveChangesAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { /* expected */ }

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LeaveQueueCommand ValidCommand() =>
        new(QueueId: QueueId, UserId: UserId);

    private static QueueEntry BuildQueueEntry() =>
        QueueEntry.Create(QueueId, UserId, ticketNumber: 1);
}

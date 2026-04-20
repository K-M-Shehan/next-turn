using FluentAssertions;
using NextTurn.Domain.Queue.Entities;

namespace NextTurn.UnitTests.Domain.Queue;

public sealed class QueueTurnNotificationAuditLogTests
{
    [Fact]
    public void Sent_CreatesSuccessfulAuditRecord()
    {
        var organisationId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var queueEntryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var log = QueueTurnNotificationAuditLog.Sent(
            organisationId,
            queueId,
            queueEntryId,
            userId,
            positionInQueue: 2,
            threshold: 3);

        var after = DateTimeOffset.UtcNow;

        log.Id.Should().NotBeEmpty();
        log.OrganisationId.Should().Be(organisationId);
        log.QueueId.Should().Be(queueId);
        log.QueueEntryId.Should().Be(queueEntryId);
        log.UserId.Should().Be(userId);
        log.PositionInQueue.Should().Be(2);
        log.Threshold.Should().Be(3);
        log.DeliveryStatus.Should().Be("Sent");
        log.ErrorMessage.Should().BeNull();
        log.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Failed_WithWhitespaceMessage_UsesDefaultMessage()
    {
        var log = QueueTurnNotificationAuditLog.Failed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            positionInQueue: 4,
            threshold: 5,
            errorMessage: "   ");

        log.DeliveryStatus.Should().Be("Failed");
        log.ErrorMessage.Should().Be("Notification delivery failed.");
    }

    [Fact]
    public void Failed_TrimsAndTruncatesErrorMessageTo500Characters()
    {
        var longMessage = " " + new string('x', 600) + " ";

        var log = QueueTurnNotificationAuditLog.Failed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            positionInQueue: 1,
            threshold: 3,
            errorMessage: longMessage);

        log.DeliveryStatus.Should().Be("Failed");
        log.ErrorMessage.Should().NotBeNull();
        log.ErrorMessage!.Length.Should().Be(500);
        log.ErrorMessage.Should().Be(new string('x', 500));
    }
}

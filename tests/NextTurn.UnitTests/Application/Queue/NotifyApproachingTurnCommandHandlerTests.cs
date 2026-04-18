using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.NotifyApproachingTurn;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.Domain.Queue.Enums;
using NextTurn.UnitTests.Helpers;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;
using QueueTurnNotificationAuditLog = NextTurn.Domain.Queue.Entities.QueueTurnNotificationAuditLog;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class NotifyApproachingTurnCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    [Fact]
    public async Task Handle_WhenQueueMissing_ThrowsDomainException()
    {
        _contextMock.Setup(c => c.Queues).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<QueueEntity>()).Object);

        var handler = new NotifyApproachingTurnCommandHandler(_contextMock.Object, _emailServiceMock.Object);
        var act = async () => await handler.Handle(new NotifyApproachingTurnCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenOrganisationMissing_ThrowsDomainException()
    {
        var queue = QueueEntity.Create(Guid.NewGuid(), "Main Queue", 100, 120);

        _contextMock.Setup(c => c.Queues).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { queue }).Object);
        _contextMock.Setup(c => c.Organisations)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<OrganisationEntity>()).Object);

        var handler = new NotifyApproachingTurnCommandHandler(_contextMock.Object, _emailServiceMock.Object);
        var act = async () => await handler.Handle(new NotifyApproachingTurnCommand(queue.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Organisation not found.");
    }

    [Fact]
    public async Task Handle_WhenNoWaitingEntries_ReturnsZeroAndDoesNotSave()
    {
        var organisation = BuildOrganisation();
        var queue = QueueEntity.Create(organisation.Id, "Main Queue", 100, 120);

        _contextMock.Setup(c => c.Queues).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { queue }).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { organisation }).Object);
        _contextMock.Setup(c => c.QueueEntries).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<QueueEntry>()).Object);

        var handler = new NotifyApproachingTurnCommandHandler(_contextMock.Object, _emailServiceMock.Object);
        var result = await handler.Handle(new NotifyApproachingTurnCommand(queue.Id), CancellationToken.None);

        result.NotificationsSent.Should().Be(0);
        result.NotificationsFailed.Should().Be(0);
        _emailServiceMock.Verify(
            s => s.SendQueueTurnApproachingEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMixOfAlreadySentSuccessAndFailure_StoresAuditsAndSaves()
    {
        var organisation = BuildOrganisation();
        organisation.SetQueueNotificationThreshold(3);
        var queue = QueueEntity.Create(organisation.Id, "Counter Queue", 100, 120);

        var userAlreadySent = User.Create(organisation.Id, "Already Sent", new EmailAddress("sent@example.com"), null, "hash");
        var userSuccess = User.Create(organisation.Id, "Success", new EmailAddress("success@example.com"), null, "hash");
        var userFailure = User.Create(organisation.Id, "Failure", new EmailAddress("fail@example.com"), null, "hash");

        var entryAlreadySent = QueueEntry.Create(queue.Id, userAlreadySent.Id, 1);
        var entrySuccess = QueueEntry.Create(queue.Id, userSuccess.Id, 2);
        var entryFailure = QueueEntry.Create(queue.Id, userFailure.Id, 3);

        var existingAudit = QueueTurnNotificationAuditLog.Sent(
            organisation.Id,
            queue.Id,
            entryAlreadySent.Id,
            userAlreadySent.Id,
            positionInQueue: 1,
            threshold: 3);

        var addedLogs = new List<QueueTurnNotificationAuditLog>();
        var auditDbSetMock = AsyncQueryableHelper.BuildMockDbSet(new[] { existingAudit });
        auditDbSetMock
            .Setup(s => s.Add(It.IsAny<QueueTurnNotificationAuditLog>()))
            .Callback<QueueTurnNotificationAuditLog>(log => addedLogs.Add(log));

        _contextMock.Setup(c => c.Queues).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { queue }).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { organisation }).Object);
        _contextMock.Setup(c => c.QueueEntries)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { entryAlreadySent, entrySuccess, entryFailure }).Object);
        _contextMock.Setup(c => c.Users)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { userAlreadySent, userSuccess, userFailure }).Object);
        _contextMock.Setup(c => c.QueueTurnNotificationAuditLogs).Returns(auditDbSetMock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _emailServiceMock
            .Setup(s => s.SendQueueTurnApproachingEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, int, int, CancellationToken>((_, _, ticket, _, _) =>
                ticket == 3
                    ? Task.FromException(new InvalidOperationException("smtp unavailable"))
                    : Task.CompletedTask);

        var handler = new NotifyApproachingTurnCommandHandler(_contextMock.Object, _emailServiceMock.Object);
        var result = await handler.Handle(new NotifyApproachingTurnCommand(queue.Id), CancellationToken.None);

        result.NotificationsSent.Should().Be(1);
        result.NotificationsFailed.Should().Be(1);

        addedLogs.Should().HaveCount(2);
        addedLogs.Should().Contain(l => l.DeliveryStatus == "Sent" && l.QueueEntryId == entrySuccess.Id);
        addedLogs.Should().Contain(l => l.DeliveryStatus == "Failed" && l.QueueEntryId == entryFailure.Id);

        _emailServiceMock.Verify(
            s => s.SendQueueTurnApproachingEmailAsync(
                It.IsAny<string>(),
                queue.Name,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIneligible_SkipsAndDoesNotSave()
    {
        var organisation = BuildOrganisation();
        organisation.SetQueueNotificationThreshold(3);
        var queue = QueueEntity.Create(organisation.Id, "Counter Queue", 100, 120);

        var disabledUser = User.Create(organisation.Id, "Disabled", new EmailAddress("disabled@example.com"), null, "hash");
        disabledUser.SetQueueTurnApproachingNotificationsEnabled(false);

        var entry = QueueEntry.Create(queue.Id, disabledUser.Id, 1);

        _contextMock.Setup(c => c.Queues).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { queue }).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { organisation }).Object);
        _contextMock.Setup(c => c.QueueEntries).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { entry }).Object);
        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { disabledUser }).Object);
        _contextMock.Setup(c => c.QueueTurnNotificationAuditLogs)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<QueueTurnNotificationAuditLog>()).Object);

        var handler = new NotifyApproachingTurnCommandHandler(_contextMock.Object, _emailServiceMock.Object);
        var result = await handler.Handle(new NotifyApproachingTurnCommand(queue.Id), CancellationToken.None);

        result.NotificationsSent.Should().Be(0);
        result.NotificationsFailed.Should().Be(0);
        _emailServiceMock.Verify(
            s => s.SendQueueTurnApproachingEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static OrganisationEntity BuildOrganisation()
    {
        return OrganisationEntity.Create(
            "City Council",
            "city-council",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@city.gov"));
    }
}

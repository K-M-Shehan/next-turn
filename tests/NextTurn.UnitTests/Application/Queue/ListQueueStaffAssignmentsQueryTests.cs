using FluentAssertions;
using Moq;
using NextTurn.Application.Queue.Queries.ListQueueStaffAssignments;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class ListQueueStaffAssignmentsQueryTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();

    [Fact]
    public async Task Handle_MapsRepositoryRowsToDto()
    {
        var queueId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        _queueRepositoryMock
            .Setup(r => r.GetStaffAssignmentsAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid StaffUserId, string Name, string Email, bool IsActive)>
            {
                (staffId, "Bob", "bob@example.com", false)
            });

        var handler = new ListQueueStaffAssignmentsQueryHandler(_queueRepositoryMock.Object);

        var result = await handler.Handle(new ListQueueStaffAssignmentsQuery(queueId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].StaffUserId.Should().Be(staffId);
        result[0].Name.Should().Be("Bob");
        result[0].Email.Should().Be("bob@example.com");
        result[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoAssignments_ReturnsEmptyList()
    {
        var queueId = Guid.NewGuid();

        _queueRepositoryMock
            .Setup(r => r.GetStaffAssignmentsAsync(queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(Guid StaffUserId, string Name, string Email, bool IsActive)>());

        var handler = new ListQueueStaffAssignmentsQueryHandler(_queueRepositoryMock.Object);

        var result = await handler.Handle(new ListQueueStaffAssignmentsQuery(queueId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Validator_WhenQueueIdEmpty_Fails()
    {
        var validator = new ListQueueStaffAssignmentsQueryValidator();

        var result = validator.Validate(new ListQueueStaffAssignmentsQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ListQueueStaffAssignmentsQuery.QueueId));
    }

    [Fact]
    public void Validator_WhenQueueIdPresent_Passes()
    {
        var validator = new ListQueueStaffAssignmentsQueryValidator();

        var result = validator.Validate(new ListQueueStaffAssignmentsQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}

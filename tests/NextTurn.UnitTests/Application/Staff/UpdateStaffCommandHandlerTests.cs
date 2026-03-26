using FluentAssertions;
using Moq;
using NextTurn.Application.Staff.Commands.UpdateStaff;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Staff;

public sealed class UpdateStaffCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly UpdateStaffCommandHandler _handler;

    public UpdateStaffCommandHandlerTests()
    {
        _handler = new UpdateStaffCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_UpdatesStaffAndAssignments()
    {
        var staff = User.Create(Guid.NewGuid(), "Staff", new EmailAddress("staff@example.com"), "0771234567", "hash", UserRole.Staff);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        _userRepositoryMock.Setup(r => r.OfficesExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var officeId = Guid.NewGuid();
        var command = new UpdateStaffCommand(staff.Id, "Updated", "0711111111", new[] { officeId }, "Counter B", TimeSpan.Parse("10:00"), TimeSpan.Parse("18:00"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Updated");
        result.OfficeIds.Should().Contain(officeId);

        _userRepositoryMock.Verify(r => r.UpdateAsync(staff, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.ReplaceStaffOfficeAssignmentsAsync(staff.Id, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotStaff_ThrowsDomainException()
    {
        var user = User.Create(Guid.NewGuid(), "User", new EmailAddress("user@example.com"), null, "hash", UserRole.User);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var command = new UpdateStaffCommand(user.Id, "Updated", null, new[] { Guid.NewGuid() }, null, null, null);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Only staff accounts can be updated from this endpoint.");
    }
}

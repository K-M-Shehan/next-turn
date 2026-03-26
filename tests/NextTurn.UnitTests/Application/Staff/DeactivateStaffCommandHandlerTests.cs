using FluentAssertions;
using Moq;
using NextTurn.Application.Staff.Commands.DeactivateStaff;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;

namespace NextTurn.UnitTests.Application.Staff;

public sealed class DeactivateStaffCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly DeactivateStaffCommandHandler _handler;

    public DeactivateStaffCommandHandlerTests()
    {
        _handler = new DeactivateStaffCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithStaffUser_DeactivatesAndDowngradesRole()
    {
        var staff = User.Create(Guid.NewGuid(), "Staff", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);

        await _handler.Handle(new DeactivateStaffCommand(staff.Id), CancellationToken.None);

        staff.IsActive.Should().BeFalse();
        staff.Role.Should().Be(UserRole.User);
        _userRepositoryMock.Verify(r => r.UpdateAsync(staff, It.IsAny<CancellationToken>()), Times.Once);
    }
}

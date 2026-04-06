using FluentAssertions;
using Moq;
using NextTurn.Application.Appointment.Commands.AssignStaffToAppointmentProfile;
using NextTurn.Application.Appointment.Commands.UnassignStaffFromAppointmentProfile;
using NextTurn.Application.Appointment.Queries.ListAppointmentProfileStaffAssignments;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class AppointmentProfileStaffAssignmentTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task AssignStaff_WhenStaffUserNotFound_Throws()
    {
        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var profileId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profileId, staffId),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Staff user not found.");
    }

    [Fact]
    public async Task AssignStaff_WhenRoleIsNotStaff_Throws()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var nonStaffUser = User.Create(orgId, "Regular User", new EmailAddress("user@example.com"), null, "hash", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonStaffUser);

        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var act = async () => await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profileId, staffId),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Only staff accounts can be assigned to appointment profiles.");
    }

    [Fact]
    public async Task AssignStaff_WhenStaffIsInactive_Throws()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var staffUser = User.Create(orgId, "Staff User", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);
        staffUser.Deactivate();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffUser);

        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var act = async () => await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profileId, staffId),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Inactive staff accounts cannot be assigned.");
    }

    [Fact]
    public async Task AssignStaff_WhenProfileNotFound_Throws()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var staffUser = User.Create(orgId, "Staff User", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffUser);

        _appointmentRepositoryMock
            .Setup(r => r.GetProfileByIdAsync(orgId, profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppointmentProfile?)null);

        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var act = async () => await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profileId, staffId),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Appointment profile not found.");
    }

    [Fact]
    public async Task AssignStaff_WhenTenantMismatch_Throws()
    {
        var staffOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var profile = AppointmentProfile.Create(otherOrgId, "Passport");
        var staffUser = User.Create(staffOrgId, "Staff User", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffUser);

        _appointmentRepositoryMock
            .Setup(r => r.GetProfileByIdAsync(staffOrgId, profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var act = async () => await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profile.Id, staffId),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Staff user belongs to a different organisation.");
    }

    [Fact]
    public async Task AssignStaff_WhenAlreadyAssigned_ReturnsWithoutSaving()
    {
        var orgId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var profile = AppointmentProfile.Create(orgId, "Passport");
        var staffUser = User.Create(orgId, "Staff User", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffUser);

        _appointmentRepositoryMock
            .Setup(r => r.GetProfileByIdAsync(orgId, profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _appointmentRepositoryMock
            .Setup(r => r.IsStaffAlreadyAssignedAsync(profile.Id, staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var result = await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profile.Id, staffId),
            CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _appointmentRepositoryMock.Verify(
            r => r.AddStaffAssignmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignStaff_WithValidData_AddsAssignmentAndSaves()
    {
        var orgId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var profile = AppointmentProfile.Create(orgId, "Passport");
        var staffUser = User.Create(orgId, "Staff User", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staffUser);

        _appointmentRepositoryMock
            .Setup(r => r.GetProfileByIdAsync(orgId, profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _appointmentRepositoryMock
            .Setup(r => r.IsStaffAlreadyAssignedAsync(profile.Id, staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new AssignStaffToAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);

        var result = await handler.Handle(
            new AssignStaffToAppointmentProfileCommand(profile.Id, staffId),
            CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _appointmentRepositoryMock.Verify(
            r => r.AddStaffAssignmentAsync(orgId, profile.Id, staffId, It.IsAny<CancellationToken>()),
            Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnassignStaff_RemovesAssignmentAndSaves()
    {
        var profileId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var handler = new UnassignStaffFromAppointmentProfileCommandHandler(
            _appointmentRepositoryMock.Object,
            _contextMock.Object);

        var result = await handler.Handle(
            new UnassignStaffFromAppointmentProfileCommand(profileId, staffId),
            CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _appointmentRepositoryMock.Verify(
            r => r.RemoveStaffAssignmentAsync(profileId, staffId, It.IsAny<CancellationToken>()),
            Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAppointmentProfileStaffAssignments_MapsRepositoryRowsToDto()
    {
        var profileId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        _appointmentRepositoryMock
            .Setup(r => r.GetStaffAssignmentsAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid StaffUserId, string Name, string Email, bool IsActive)>
            {
                (staffId, "Alice", "alice@example.com", true)
            });

        var handler = new ListAppointmentProfileStaffAssignmentsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(
            new ListAppointmentProfileStaffAssignmentsQuery(profileId),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].StaffUserId.Should().Be(staffId);
        result[0].Name.Should().Be("Alice");
        result[0].Email.Should().Be("alice@example.com");
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public void AssignStaffValidator_WhenIdsAreEmpty_Fails()
    {
        var validator = new AssignStaffToAppointmentProfileCommandValidator();

        var result = validator.Validate(new AssignStaffToAppointmentProfileCommand(Guid.Empty, Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void AssignStaffValidator_WhenIdsArePresent_Passes()
    {
        var validator = new AssignStaffToAppointmentProfileCommandValidator();

        var result = validator.Validate(new AssignStaffToAppointmentProfileCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UnassignStaffValidator_WhenIdsAreEmpty_Fails()
    {
        var validator = new UnassignStaffFromAppointmentProfileCommandValidator();

        var result = validator.Validate(new UnassignStaffFromAppointmentProfileCommand(Guid.Empty, Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void UnassignStaffValidator_WhenIdsArePresent_Passes()
    {
        var validator = new UnassignStaffFromAppointmentProfileCommandValidator();

        var result = validator.Validate(new UnassignStaffFromAppointmentProfileCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListAppointmentProfileStaffAssignmentsValidator_WhenIdIsEmpty_Fails()
    {
        var validator = new ListAppointmentProfileStaffAssignmentsQueryValidator();

        var result = validator.Validate(new ListAppointmentProfileStaffAssignmentsQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListAppointmentProfileStaffAssignmentsValidator_WhenIdIsPresent_Passes()
    {
        var validator = new ListAppointmentProfileStaffAssignmentsQueryValidator();

        var result = validator.Validate(new ListAppointmentProfileStaffAssignmentsQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}

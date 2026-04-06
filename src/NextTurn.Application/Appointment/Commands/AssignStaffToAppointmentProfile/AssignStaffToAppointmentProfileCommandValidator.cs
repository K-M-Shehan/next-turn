using FluentValidation;

namespace NextTurn.Application.Appointment.Commands.AssignStaffToAppointmentProfile;

public sealed class AssignStaffToAppointmentProfileCommandValidator : AbstractValidator<AssignStaffToAppointmentProfileCommand>
{
    public AssignStaffToAppointmentProfileCommandValidator()
    {
        RuleFor(x => x.AppointmentProfileId).NotEmpty();
        RuleFor(x => x.StaffUserId).NotEmpty();
    }
}
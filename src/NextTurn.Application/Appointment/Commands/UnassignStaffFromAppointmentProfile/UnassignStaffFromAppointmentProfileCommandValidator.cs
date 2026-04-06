using FluentValidation;

namespace NextTurn.Application.Appointment.Commands.UnassignStaffFromAppointmentProfile;

public sealed class UnassignStaffFromAppointmentProfileCommandValidator : AbstractValidator<UnassignStaffFromAppointmentProfileCommand>
{
    public UnassignStaffFromAppointmentProfileCommandValidator()
    {
        RuleFor(x => x.AppointmentProfileId).NotEmpty();
        RuleFor(x => x.StaffUserId).NotEmpty();
    }
}
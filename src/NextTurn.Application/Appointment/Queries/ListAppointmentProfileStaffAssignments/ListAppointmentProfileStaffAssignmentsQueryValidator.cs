using FluentValidation;

namespace NextTurn.Application.Appointment.Queries.ListAppointmentProfileStaffAssignments;

public sealed class ListAppointmentProfileStaffAssignmentsQueryValidator : AbstractValidator<ListAppointmentProfileStaffAssignmentsQuery>
{
    public ListAppointmentProfileStaffAssignmentsQueryValidator()
    {
        RuleFor(x => x.AppointmentProfileId).NotEmpty();
    }
}
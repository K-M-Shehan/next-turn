using MediatR;
using NextTurn.Application.Appointment.Common;

namespace NextTurn.Application.Appointment.Queries.ListAppointmentProfileStaffAssignments;

public sealed record ListAppointmentProfileStaffAssignmentsQuery(Guid AppointmentProfileId)
    : IRequest<IReadOnlyList<AppointmentStaffAssignmentDto>>;
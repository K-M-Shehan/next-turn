using MediatR;
using NextTurn.Application.Appointment.Common;
using NextTurn.Domain.Appointment.Repositories;

namespace NextTurn.Application.Appointment.Queries.ListAppointmentProfileStaffAssignments;

public sealed class ListAppointmentProfileStaffAssignmentsQueryHandler
    : IRequestHandler<ListAppointmentProfileStaffAssignmentsQuery, IReadOnlyList<AppointmentStaffAssignmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public ListAppointmentProfileStaffAssignmentsQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<IReadOnlyList<AppointmentStaffAssignmentDto>> Handle(
        ListAppointmentProfileStaffAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var assigned = await _appointmentRepository.GetStaffAssignmentsAsync(request.AppointmentProfileId, cancellationToken);

        return assigned
            .Select(a => new AppointmentStaffAssignmentDto(a.StaffUserId, a.Name, a.Email, a.IsActive))
            .ToList();
    }
}
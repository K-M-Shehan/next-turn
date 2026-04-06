using MediatR;

namespace NextTurn.Application.Appointment.Commands.UnassignStaffFromAppointmentProfile;

public sealed record UnassignStaffFromAppointmentProfileCommand(Guid AppointmentProfileId, Guid StaffUserId) : IRequest<Unit>;
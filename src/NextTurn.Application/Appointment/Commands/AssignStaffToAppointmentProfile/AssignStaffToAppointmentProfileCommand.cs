using MediatR;

namespace NextTurn.Application.Appointment.Commands.AssignStaffToAppointmentProfile;

public sealed record AssignStaffToAppointmentProfileCommand(Guid AppointmentProfileId, Guid StaffUserId) : IRequest<Unit>;
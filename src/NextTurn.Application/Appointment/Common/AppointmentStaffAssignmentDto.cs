namespace NextTurn.Application.Appointment.Common;

public sealed record AppointmentStaffAssignmentDto(
    Guid StaffUserId,
    string Name,
    string Email,
    bool IsActive);
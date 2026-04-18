using MediatR;

namespace NextTurn.Application.Appointment.Commands.BookAppointment;

/// <summary>
/// In-process event published after an appointment is successfully booked.
/// </summary>
public sealed record AppointmentBookedNotification(Guid AppointmentId) : INotification;

using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;

namespace NextTurn.Application.Appointment.Commands.UnassignStaffFromAppointmentProfile;

public sealed class UnassignStaffFromAppointmentProfileCommandHandler : IRequestHandler<UnassignStaffFromAppointmentProfileCommand, Unit>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IApplicationDbContext _context;

    public UnassignStaffFromAppointmentProfileCommandHandler(
        IAppointmentRepository appointmentRepository,
        IApplicationDbContext context)
    {
        _appointmentRepository = appointmentRepository;
        _context = context;
    }

    public async Task<Unit> Handle(UnassignStaffFromAppointmentProfileCommand request, CancellationToken cancellationToken)
    {
        await _appointmentRepository.RemoveStaffAssignmentAsync(
            request.AppointmentProfileId,
            request.StaffUserId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
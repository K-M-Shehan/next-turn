using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Appointment.Commands.AssignStaffToAppointmentProfile;

public sealed class AssignStaffToAppointmentProfileCommandHandler : IRequestHandler<AssignStaffToAppointmentProfileCommand, Unit>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _context;

    public AssignStaffToAppointmentProfileCommandHandler(
        IAppointmentRepository appointmentRepository,
        IUserRepository userRepository,
        IApplicationDbContext context)
    {
        _appointmentRepository = appointmentRepository;
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<Unit> Handle(AssignStaffToAppointmentProfileCommand request, CancellationToken cancellationToken)
    {
        var staffUser = await _userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);
        if (staffUser is null)
            throw new DomainException("Staff user not found.");

        if (staffUser.Role != UserRole.Staff)
            throw new DomainException("Only staff accounts can be assigned to appointment profiles.");

        if (!staffUser.IsActive)
            throw new DomainException("Inactive staff accounts cannot be assigned.");

        var profile = await _appointmentRepository.GetProfileByIdAsync(
            staffUser.TenantId,
            request.AppointmentProfileId,
            cancellationToken);

        if (profile is null)
            throw new DomainException("Appointment profile not found.");

        if (staffUser.TenantId != profile.OrganisationId)
            throw new DomainException("Staff user belongs to a different organisation.");

        var alreadyAssigned = await _appointmentRepository.IsStaffAlreadyAssignedAsync(
            request.AppointmentProfileId,
            request.StaffUserId,
            cancellationToken);

        if (alreadyAssigned)
            return Unit.Value;

        await _appointmentRepository.AddStaffAssignmentAsync(
            profile.OrganisationId,
            request.AppointmentProfileId,
            request.StaffUserId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
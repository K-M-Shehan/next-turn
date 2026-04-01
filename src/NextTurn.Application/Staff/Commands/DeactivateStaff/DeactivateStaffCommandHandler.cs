using MediatR;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Staff.Commands.DeactivateStaff;

public sealed class DeactivateStaffCommandHandler : IRequestHandler<DeactivateStaffCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public DeactivateStaffCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(DeactivateStaffCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);
        if (user is null)
            throw new DomainException("Staff user not found.");

        if (user.Role != UserRole.Staff)
            throw new DomainException("Only staff accounts can be deactivated from this endpoint.");

        user.DeactivateStaffAccess();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}

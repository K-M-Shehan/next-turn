using MediatR;
using NextTurn.Application.Staff.Common;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Staff.Commands.UpdateStaff;

public sealed class UpdateStaffCommandHandler : IRequestHandler<UpdateStaffCommand, StaffDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateStaffCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<StaffDto> Handle(UpdateStaffCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);
        if (user is null)
            throw new DomainException("Staff user not found.");

        if (user.Role != UserRole.Staff)
            throw new DomainException("Only staff accounts can be updated from this endpoint.");

        var officesExist = await _userRepository.OfficesExistAsync(request.OfficeIds, cancellationToken);
        if (!officesExist)
            throw new DomainException("One or more offices were not found for this tenant.");

        user.UpdateStaffProfile(
            request.Name,
            request.Phone,
            request.CounterName,
            request.ShiftStart,
            request.ShiftEnd);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.ReplaceStaffOfficeAssignmentsAsync(user.Id, request.OfficeIds, cancellationToken);

        return new StaffDto(
            user.Id,
            user.Name,
            user.Email.Value,
            user.Phone,
            user.IsActive,
            user.CounterName,
            ToShiftString(user.ShiftStart),
            ToShiftString(user.ShiftEnd),
            request.OfficeIds.Distinct().ToList(),
            user.CreatedAt);
    }

    private static string? ToShiftString(TimeSpan? value)
    {
        return value.HasValue ? value.Value.ToString(@"hh\:mm") : null;
    }
}

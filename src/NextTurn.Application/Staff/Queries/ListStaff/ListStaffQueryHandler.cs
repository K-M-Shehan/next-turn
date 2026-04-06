using MediatR;
using NextTurn.Application.Staff.Common;
using NextTurn.Domain.Auth.Repositories;

namespace NextTurn.Application.Staff.Queries.ListStaff;

public sealed class ListStaffQueryHandler : IRequestHandler<ListStaffQuery, ListStaffResult>
{
    private readonly IUserRepository _userRepository;

    public ListStaffQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ListStaffResult> Handle(ListStaffQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _userRepository.ListStaffPagedAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var assignmentMap = await _userRepository.GetAssignedOfficeIdsByStaffUserIdsAsync(
            items.Select(x => x.Id).ToList(),
            cancellationToken);

        var data = items.Select(user => new StaffDto(
            user.Id,
            user.Name,
            user.Email.Value,
            user.Phone,
            user.IsActive,
            user.CounterName,
            ToShiftString(user.ShiftStart),
            ToShiftString(user.ShiftEnd),
            assignmentMap.TryGetValue(user.Id, out var officeIds) ? officeIds : [],
            user.CreatedAt)).ToList();

        return new ListStaffResult(data, request.PageNumber, request.PageSize, totalCount);
    }

    private static string? ToShiftString(TimeSpan? value)
    {
        return value.HasValue ? value.Value.ToString(@"hh\:mm") : null;
    }
}

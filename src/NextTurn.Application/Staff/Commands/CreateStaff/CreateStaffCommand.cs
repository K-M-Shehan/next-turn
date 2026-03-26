using MediatR;
using NextTurn.Application.Staff.Common;

namespace NextTurn.Application.Staff.Commands.CreateStaff;

public sealed record CreateStaffCommand(
    string Name,
    string Email,
    string? Phone,
    IReadOnlyList<Guid> OfficeIds,
    string? CounterName,
    TimeSpan? ShiftStart,
    TimeSpan? ShiftEnd) : IRequest<StaffDto>;

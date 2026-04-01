using NextTurn.Application.Staff.Common;

namespace NextTurn.Application.Staff.Queries.ListStaff;

public sealed record ListStaffResult(
    IReadOnlyList<StaffDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

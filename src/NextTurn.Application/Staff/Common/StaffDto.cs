namespace NextTurn.Application.Staff.Common;

public sealed record StaffDto(
    Guid StaffUserId,
    string Name,
    string Email,
    string? Phone,
    bool IsActive,
    string? CounterName,
    string? ShiftStart,
    string? ShiftEnd,
    IReadOnlyList<Guid> OfficeIds,
    DateTimeOffset CreatedAt);

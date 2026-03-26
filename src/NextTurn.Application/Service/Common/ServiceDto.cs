namespace NextTurn.Application.Service.Common;

public sealed record ServiceDto(
    Guid ServiceId,
    string Name,
    string Code,
    string Description,
    int EstimatedDurationMinutes,
    bool IsActive,
    IReadOnlyList<Guid> AssignedOfficeIds,
    DateTimeOffset? DeactivatedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

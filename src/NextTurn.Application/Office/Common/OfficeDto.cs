namespace NextTurn.Application.Office.Common;

public sealed record OfficeDto(
    Guid OfficeId,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    string OpeningHours,
    bool IsActive,
    DateTimeOffset? DeactivatedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

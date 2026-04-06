namespace NextTurn.API.Models.Offices;

public sealed class UpdateOfficeRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string OpeningHours { get; init; } = string.Empty;
}

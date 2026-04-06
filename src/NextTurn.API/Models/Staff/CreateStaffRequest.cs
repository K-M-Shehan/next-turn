namespace NextTurn.API.Models.Staff;

public sealed class CreateStaffRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public IReadOnlyList<Guid> OfficeIds { get; set; } = [];
    public string? CounterName { get; set; }
    public string? ShiftStart { get; set; }
    public string? ShiftEnd { get; set; }
}

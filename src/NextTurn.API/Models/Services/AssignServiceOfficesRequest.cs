namespace NextTurn.API.Models.Services;

public sealed class AssignServiceOfficesRequest
{
    public IReadOnlyList<Guid> OfficeIds { get; set; } = [];
}

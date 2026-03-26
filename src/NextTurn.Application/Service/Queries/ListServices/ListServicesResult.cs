using NextTurn.Application.Service.Common;

namespace NextTurn.Application.Service.Queries.ListServices;

public sealed record ListServicesResult(
    IReadOnlyList<ServiceDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

using NextTurn.Application.Office.Common;

namespace NextTurn.Application.Office.Queries.ListOffices;

public sealed record ListOfficesResult(
    IReadOnlyList<OfficeDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

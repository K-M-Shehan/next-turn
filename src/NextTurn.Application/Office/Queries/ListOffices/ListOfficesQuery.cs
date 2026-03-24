using MediatR;

namespace NextTurn.Application.Office.Queries.ListOffices;

public sealed record ListOfficesQuery(
    Guid OrganisationId,
    bool? IsActive,
    string? Search,
    int PageNumber,
    int PageSize) : IRequest<ListOfficesResult>;

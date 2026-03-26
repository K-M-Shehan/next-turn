using MediatR;

namespace NextTurn.Application.Service.Queries.ListServices;

public sealed record ListServicesQuery(
    Guid OrganisationId,
    bool ActiveOnly,
    int PageNumber,
    int PageSize) : IRequest<ListServicesResult>;

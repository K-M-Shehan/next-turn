using MediatR;
using NextTurn.Application.Office.Common;

namespace NextTurn.Application.Office.Queries.GetOfficeById;

public sealed record GetOfficeByIdQuery(Guid OrganisationId, Guid OfficeId) : IRequest<OfficeDto>;

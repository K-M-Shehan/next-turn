using MediatR;
using NextTurn.Application.Office.Common;

namespace NextTurn.Application.Office.Commands.UpdateOffice;

public sealed record UpdateOfficeCommand(
    Guid OrganisationId,
    Guid OfficeId,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    string OpeningHours) : IRequest<OfficeDto>;

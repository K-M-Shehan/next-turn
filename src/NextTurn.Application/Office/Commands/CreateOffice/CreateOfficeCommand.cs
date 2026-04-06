using MediatR;
using NextTurn.Application.Office.Common;

namespace NextTurn.Application.Office.Commands.CreateOffice;

public sealed record CreateOfficeCommand(
    Guid OrganisationId,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    string OpeningHours) : IRequest<OfficeDto>;

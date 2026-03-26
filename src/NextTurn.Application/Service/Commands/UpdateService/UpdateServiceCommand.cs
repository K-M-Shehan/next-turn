using MediatR;
using NextTurn.Application.Service.Common;

namespace NextTurn.Application.Service.Commands.UpdateService;

public sealed record UpdateServiceCommand(
    Guid OrganisationId,
    Guid ServiceId,
    string Name,
    string Description,
    int EstimatedDurationMinutes) : IRequest<ServiceDto>;

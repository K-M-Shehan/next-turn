using MediatR;
using NextTurn.Application.Service.Common;

namespace NextTurn.Application.Service.Commands.CreateService;

public sealed record CreateServiceCommand(
    Guid OrganisationId,
    string Name,
    string Code,
    string Description,
    int EstimatedDurationMinutes,
    bool IsActive) : IRequest<ServiceDto>;

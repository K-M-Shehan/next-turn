using MediatR;

namespace NextTurn.Application.Service.Commands.DeactivateService;

public sealed record DeactivateServiceCommand(Guid OrganisationId, Guid ServiceId) : IRequest<Unit>;

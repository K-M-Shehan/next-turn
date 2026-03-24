using MediatR;

namespace NextTurn.Application.Office.Commands.DeactivateOffice;

public sealed record DeactivateOfficeCommand(Guid OrganisationId, Guid OfficeId) : IRequest<Unit>;

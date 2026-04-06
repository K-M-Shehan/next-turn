using MediatR;

namespace NextTurn.Application.Service.Commands.AssignServiceOffices;

public sealed record AssignServiceOfficesCommand(
    Guid OrganisationId,
    Guid ServiceId,
    IReadOnlyList<Guid> OfficeIds) : IRequest<Unit>;

using MediatR;

namespace NextTurn.Application.Service.Commands.RemoveServiceOfficeAssignment;

public sealed record RemoveServiceOfficeAssignmentCommand(
    Guid OrganisationId,
    Guid ServiceId,
    Guid OfficeId) : IRequest<Unit>;

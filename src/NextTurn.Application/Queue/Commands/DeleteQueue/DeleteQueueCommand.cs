using MediatR;

namespace NextTurn.Application.Queue.Commands.DeleteQueue;

public sealed record DeleteQueueCommand(Guid OrganisationId, Guid QueueId) : IRequest<Unit>;

using MediatR;
using NextTurn.Application.Queue.Commands;

namespace NextTurn.Application.Queue.Commands.ServeNext;

public sealed record ServeNextCommand(Guid QueueId, Guid PerformedByUserId) : IRequest<QueueEntryActionResult>;
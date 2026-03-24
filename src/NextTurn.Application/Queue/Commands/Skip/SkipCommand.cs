using MediatR;
using NextTurn.Application.Queue.Commands;

namespace NextTurn.Application.Queue.Commands.Skip;

public sealed record SkipCommand(
    Guid QueueId,
    Guid PerformedByUserId,
    string? Reason) : IRequest<QueueEntryActionResult>;
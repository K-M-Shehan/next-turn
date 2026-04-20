using MediatR;

namespace NextTurn.Application.Queue.Commands.NotifyApproachingTurn;

public sealed record NotifyApproachingTurnCommand(Guid QueueId) : IRequest<NotifyApproachingTurnResult>;

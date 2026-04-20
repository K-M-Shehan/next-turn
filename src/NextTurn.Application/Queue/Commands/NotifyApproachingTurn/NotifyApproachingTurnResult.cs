namespace NextTurn.Application.Queue.Commands.NotifyApproachingTurn;

public sealed record NotifyApproachingTurnResult(int NotificationsSent, int NotificationsFailed);

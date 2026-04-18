using MediatR;

namespace NextTurn.Application.Auth.Commands.UpdateQueueNotificationPreference;

public sealed record UpdateQueueNotificationPreferenceCommand(
    Guid UserId,
    bool QueueTurnApproachingNotificationsEnabled) : IRequest<Unit>;

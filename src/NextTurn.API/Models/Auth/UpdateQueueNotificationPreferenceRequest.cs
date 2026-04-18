namespace NextTurn.API.Models.Auth;

public sealed record UpdateQueueNotificationPreferenceRequest(
    bool QueueTurnApproachingNotificationsEnabled);

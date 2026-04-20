using MediatR;

namespace NextTurn.Application.Auth.Queries.GetQueueNotificationPreference;

public sealed record GetQueueNotificationPreferenceQuery(Guid UserId) : IRequest<QueueNotificationPreferenceResult>;

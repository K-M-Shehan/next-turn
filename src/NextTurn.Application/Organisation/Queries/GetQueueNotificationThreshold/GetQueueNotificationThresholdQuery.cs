using MediatR;

namespace NextTurn.Application.Organisation.Queries.GetQueueNotificationThreshold;

public sealed record GetQueueNotificationThresholdQuery(Guid OrganisationId) : IRequest<QueueNotificationThresholdResult>;

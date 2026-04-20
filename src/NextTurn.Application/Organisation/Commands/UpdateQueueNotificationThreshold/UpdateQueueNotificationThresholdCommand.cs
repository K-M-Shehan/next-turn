using MediatR;

namespace NextTurn.Application.Organisation.Commands.UpdateQueueNotificationThreshold;

public sealed record UpdateQueueNotificationThresholdCommand(
    Guid OrganisationId,
    int QueueNotificationThreshold) : IRequest<Unit>;

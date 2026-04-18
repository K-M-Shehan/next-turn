using FluentValidation;

namespace NextTurn.Application.Organisation.Commands.UpdateQueueNotificationThreshold;

public sealed class UpdateQueueNotificationThresholdCommandValidator : AbstractValidator<UpdateQueueNotificationThresholdCommand>
{
    public UpdateQueueNotificationThresholdCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.QueueNotificationThreshold)
            .InclusiveBetween(1, 50)
            .WithMessage("Queue notification threshold must be between 1 and 50.");
    }
}

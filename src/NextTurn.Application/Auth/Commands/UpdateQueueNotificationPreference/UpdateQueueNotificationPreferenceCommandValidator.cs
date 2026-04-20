using FluentValidation;

namespace NextTurn.Application.Auth.Commands.UpdateQueueNotificationPreference;

public sealed class UpdateQueueNotificationPreferenceCommandValidator : AbstractValidator<UpdateQueueNotificationPreferenceCommand>
{
    public UpdateQueueNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}

using FluentValidation;

namespace NextTurn.Application.Notification.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("Notification ID is required.");
    }
}

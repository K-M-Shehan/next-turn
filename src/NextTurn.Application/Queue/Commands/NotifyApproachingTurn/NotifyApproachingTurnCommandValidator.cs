using FluentValidation;

namespace NextTurn.Application.Queue.Commands.NotifyApproachingTurn;

public sealed class NotifyApproachingTurnCommandValidator : AbstractValidator<NotifyApproachingTurnCommand>
{
    public NotifyApproachingTurnCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}

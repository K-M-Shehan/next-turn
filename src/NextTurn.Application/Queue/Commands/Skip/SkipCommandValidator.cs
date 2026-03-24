using FluentValidation;

namespace NextTurn.Application.Queue.Commands.Skip;

public sealed class SkipCommandValidator : AbstractValidator<SkipCommand>
{
    public SkipCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");

        RuleFor(x => x.PerformedByUserId)
            .NotEmpty().WithMessage("PerformedBy user ID is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(200)
            .WithMessage("Reason must not exceed 200 characters.");
    }
}
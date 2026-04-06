using FluentValidation;

namespace NextTurn.Application.Queue.Commands.ServeNext;

public sealed class ServeNextCommandValidator : AbstractValidator<ServeNextCommand>
{
    public ServeNextCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");

        RuleFor(x => x.PerformedByUserId)
            .NotEmpty().WithMessage("PerformedBy user ID is required.");
    }
}
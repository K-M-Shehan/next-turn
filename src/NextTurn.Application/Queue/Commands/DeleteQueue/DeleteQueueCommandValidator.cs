using FluentValidation;

namespace NextTurn.Application.Queue.Commands.DeleteQueue;

public sealed class DeleteQueueCommandValidator : AbstractValidator<DeleteQueueCommand>
{
    public DeleteQueueCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}

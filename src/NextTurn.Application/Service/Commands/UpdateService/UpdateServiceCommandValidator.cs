using FluentValidation;

namespace NextTurn.Application.Service.Commands.UpdateService;

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(120).WithMessage("Service name must not exceed 120 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Service description is required.")
            .MaximumLength(500).WithMessage("Service description must not exceed 500 characters.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .InclusiveBetween(1, 1440).WithMessage("Estimated duration must be between 1 and 1440 minutes.");
    }
}

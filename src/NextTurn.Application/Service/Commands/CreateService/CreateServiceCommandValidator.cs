using FluentValidation;

namespace NextTurn.Application.Service.Commands.CreateService;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(120).WithMessage("Service name must not exceed 120 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Service code is required.")
            .MaximumLength(40).WithMessage("Service code must not exceed 40 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Service description is required.")
            .MaximumLength(500).WithMessage("Service description must not exceed 500 characters.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .InclusiveBetween(1, 1440).WithMessage("Estimated duration must be between 1 and 1440 minutes.");
    }
}

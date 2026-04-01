using FluentValidation;

namespace NextTurn.Application.Service.Commands.DeactivateService;

public sealed class DeactivateServiceCommandValidator : AbstractValidator<DeactivateServiceCommand>
{
    public DeactivateServiceCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required.");
    }
}

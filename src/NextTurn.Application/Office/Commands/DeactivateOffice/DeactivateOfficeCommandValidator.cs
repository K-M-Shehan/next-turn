using FluentValidation;

namespace NextTurn.Application.Office.Commands.DeactivateOffice;

public sealed class DeactivateOfficeCommandValidator : AbstractValidator<DeactivateOfficeCommand>
{
    public DeactivateOfficeCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office ID is required.");
    }
}

using FluentValidation;

namespace NextTurn.Application.Service.Commands.AssignServiceOffices;

public sealed class AssignServiceOfficesCommandValidator : AbstractValidator<AssignServiceOfficesCommand>
{
    public AssignServiceOfficesCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required.");

        RuleFor(x => x.OfficeIds)
            .NotNull().WithMessage("Office IDs are required.")
            .Must(x => x.Count > 0).WithMessage("At least one office ID is required.");

        RuleForEach(x => x.OfficeIds)
            .NotEmpty().WithMessage("Office ID cannot be empty.");
    }
}

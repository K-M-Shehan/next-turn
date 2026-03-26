using FluentValidation;

namespace NextTurn.Application.Service.Commands.RemoveServiceOfficeAssignment;

public sealed class RemoveServiceOfficeAssignmentCommandValidator : AbstractValidator<RemoveServiceOfficeAssignmentCommand>
{
    public RemoveServiceOfficeAssignmentCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required.");

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office ID is required.");
    }
}

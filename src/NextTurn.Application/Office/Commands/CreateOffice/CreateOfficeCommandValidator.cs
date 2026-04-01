using FluentValidation;

namespace NextTurn.Application.Office.Commands.CreateOffice;

public sealed class CreateOfficeCommandValidator : AbstractValidator<CreateOfficeCommand>
{
    public CreateOfficeCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Office name is required.")
            .MaximumLength(120).WithMessage("Office name must not exceed 120 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Office address is required.")
            .MaximumLength(300).WithMessage("Office address must not exceed 300 characters.");

        RuleFor(x => x.OpeningHours)
            .NotEmpty().WithMessage("Opening hours are required.")
            .MaximumLength(4000).WithMessage("Opening hours must not exceed 4000 characters.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}

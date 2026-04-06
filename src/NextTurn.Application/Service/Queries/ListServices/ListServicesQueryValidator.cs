using FluentValidation;

namespace NextTurn.Application.Service.Queries.ListServices;

public sealed class ListServicesQueryValidator : AbstractValidator<ListServicesQuery>
{
    public ListServicesQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}

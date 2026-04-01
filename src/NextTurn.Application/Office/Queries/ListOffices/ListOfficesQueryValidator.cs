using FluentValidation;

namespace NextTurn.Application.Office.Queries.ListOffices;

public sealed class ListOfficesQueryValidator : AbstractValidator<ListOfficesQuery>
{
    public ListOfficesQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search must not exceed 200 characters.");
    }
}

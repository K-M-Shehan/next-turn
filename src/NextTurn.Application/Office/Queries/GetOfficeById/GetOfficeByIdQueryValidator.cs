using FluentValidation;

namespace NextTurn.Application.Office.Queries.GetOfficeById;

public sealed class GetOfficeByIdQueryValidator : AbstractValidator<GetOfficeByIdQuery>
{
    public GetOfficeByIdQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.OfficeId)
            .NotEmpty().WithMessage("Office ID is required.");
    }
}

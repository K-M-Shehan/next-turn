using FluentValidation;

namespace NextTurn.Application.Queue.Queries.GetDailySummaryReport;

public sealed class DailySummaryReportQueryValidator : AbstractValidator<DailySummaryReportQuery>
{
    public DailySummaryReportQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty();

        RuleFor(x => x.Date)
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Date cannot be far in the future.");
    }
}

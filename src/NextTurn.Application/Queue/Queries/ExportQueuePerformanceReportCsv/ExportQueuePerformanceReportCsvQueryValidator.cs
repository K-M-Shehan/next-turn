using FluentValidation;

namespace NextTurn.Application.Queue.Queries.ExportQueuePerformanceReportCsv;

public sealed class ExportQueuePerformanceReportCsvQueryValidator : AbstractValidator<ExportQueuePerformanceReportCsvQuery>
{
    public ExportQueuePerformanceReportCsvQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty();

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("Start date must be on or before end date.");

        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("Date range must be 366 days or less.");
    }
}

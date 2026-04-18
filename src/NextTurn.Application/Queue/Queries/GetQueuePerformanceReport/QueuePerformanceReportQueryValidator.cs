using FluentValidation;

namespace NextTurn.Application.Queue.Queries.GetQueuePerformanceReport;

public sealed class QueuePerformanceReportQueryValidator : AbstractValidator<QueuePerformanceReportQuery>
{
    public QueuePerformanceReportQueryValidator()
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

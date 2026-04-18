using MediatR;
using NextTurn.Application.Queue.DailySummary;

namespace NextTurn.Application.Queue.Queries.GetDailySummaryReport;

public sealed record DailySummaryReportQuery(
    Guid OrganisationId,
    DateOnly Date) : IRequest<DailyQueueSummaryReportResult>;

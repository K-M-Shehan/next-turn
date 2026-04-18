using MediatR;
using NextTurn.Application.Queue.DailySummary;

namespace NextTurn.Application.Queue.Queries.GetDailySummaryReport;

public sealed class DailySummaryReportQueryHandler : IRequestHandler<DailySummaryReportQuery, DailyQueueSummaryReportResult>
{
    private readonly IDailyQueueSummaryReportService _dailySummaryService;

    public DailySummaryReportQueryHandler(IDailyQueueSummaryReportService dailySummaryService)
    {
        _dailySummaryService = dailySummaryService;
    }

    public Task<DailyQueueSummaryReportResult> Handle(
        DailySummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        return _dailySummaryService.GenerateAsync(
            request.OrganisationId,
            request.Date,
            cancellationToken);
    }
}

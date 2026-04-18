namespace NextTurn.Application.Queue.DailySummary;

public sealed record DailyQueueSummaryReportResult(
    DateOnly Date,
    DateOnly PreviousDayDate,
    DateOnly PreviousWeekDate,
    int TotalServed,
    int TotalSkipped,
    int TotalNoShows,
    DailyQueueMetricTrend TotalServedTrend,
    DailyQueueMetricTrend TotalSkippedTrend,
    DailyQueueMetricTrend TotalNoShowsTrend,
    IReadOnlyList<DailyQueueSummaryRow> Rows);

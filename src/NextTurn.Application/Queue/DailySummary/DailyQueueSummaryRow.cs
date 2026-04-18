namespace NextTurn.Application.Queue.DailySummary;

public sealed record DailyQueueSummaryRow(
    Guid OfficeId,
    string OfficeName,
    Guid ServiceId,
    string ServiceName,
    int Served,
    int Skipped,
    int NoShows,
    DailyQueueMetricTrend ServedTrend,
    DailyQueueMetricTrend SkippedTrend,
    DailyQueueMetricTrend NoShowsTrend);

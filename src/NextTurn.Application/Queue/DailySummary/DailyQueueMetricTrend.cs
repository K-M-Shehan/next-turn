namespace NextTurn.Application.Queue.DailySummary;

public sealed record DailyQueueMetricTrend(
    int PreviousDay,
    int PreviousWeek,
    int DeltaFromPreviousDay,
    int DeltaFromPreviousWeek,
    double ChangePercentFromPreviousDay,
    double ChangePercentFromPreviousWeek);

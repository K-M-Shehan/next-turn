namespace NextTurn.Application.Queue.Reports;

public sealed record QueuePerformanceDataPoint(
    DateTimeOffset JoinedAt,
    DateTimeOffset ServedAt,
    int HourOfDay);

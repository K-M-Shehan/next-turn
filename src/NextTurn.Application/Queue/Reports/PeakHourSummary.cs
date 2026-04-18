namespace NextTurn.Application.Queue.Reports;

public sealed record PeakHourSummary(
    int HourOfDay,
    int ServedCount);

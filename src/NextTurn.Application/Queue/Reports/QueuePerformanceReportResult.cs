namespace NextTurn.Application.Queue.Reports;

public sealed record QueuePerformanceReportResult(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ServiceId,
    Guid? OfficeId,
    int TotalServed,
    double AverageWaitMinutes,
    IReadOnlyList<PeakHourSummary> PeakHours);

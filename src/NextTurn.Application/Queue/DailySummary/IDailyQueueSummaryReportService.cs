namespace NextTurn.Application.Queue.DailySummary;

public interface IDailyQueueSummaryReportService
{
    Task<DailyQueueSummaryReportResult> GenerateAsync(
        Guid organisationId,
        DateOnly date,
        CancellationToken cancellationToken);
}

namespace NextTurn.Application.Queue.Reports;

public interface IQueuePerformanceReportService
{
    Task<QueuePerformanceReportResult> GenerateAsync(
        QueuePerformanceFilter filter,
        CancellationToken cancellationToken);
}

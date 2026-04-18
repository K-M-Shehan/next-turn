namespace NextTurn.Application.Queue.Reports;

public interface IQueuePerformanceExportService
{
    byte[] ExportCsv(QueuePerformanceReportResult report);
}

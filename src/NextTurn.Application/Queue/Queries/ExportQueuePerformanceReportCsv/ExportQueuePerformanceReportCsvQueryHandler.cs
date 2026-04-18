using MediatR;
using NextTurn.Application.Queue.Reports;

namespace NextTurn.Application.Queue.Queries.ExportQueuePerformanceReportCsv;

public sealed class ExportQueuePerformanceReportCsvQueryHandler
    : IRequestHandler<ExportQueuePerformanceReportCsvQuery, QueuePerformanceCsvExportResult>
{
    private readonly IQueuePerformanceReportService _reportService;
    private readonly IQueuePerformanceExportService _exportService;

    public ExportQueuePerformanceReportCsvQueryHandler(
        IQueuePerformanceReportService reportService,
        IQueuePerformanceExportService exportService)
    {
        _reportService = reportService;
        _exportService = exportService;
    }

    public async Task<QueuePerformanceCsvExportResult> Handle(
        ExportQueuePerformanceReportCsvQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new QueuePerformanceFilter(
            request.OrganisationId,
            request.StartDate,
            request.EndDate,
            request.ServiceId,
            request.OfficeId);

        var report = await _reportService.GenerateAsync(filter, cancellationToken);
        var content = _exportService.ExportCsv(report);
        var fileName = $"queue-performance-{request.StartDate:yyyyMMdd}-{request.EndDate:yyyyMMdd}.csv";

        return new QueuePerformanceCsvExportResult(fileName, "text/csv", content);
    }
}

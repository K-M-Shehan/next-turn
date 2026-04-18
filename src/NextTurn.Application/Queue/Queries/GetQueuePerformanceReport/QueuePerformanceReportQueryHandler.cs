using MediatR;
using NextTurn.Application.Queue.Reports;

namespace NextTurn.Application.Queue.Queries.GetQueuePerformanceReport;

public sealed class QueuePerformanceReportQueryHandler
    : IRequestHandler<QueuePerformanceReportQuery, QueuePerformanceReportResult>
{
    private readonly IQueuePerformanceReportService _reportService;

    public QueuePerformanceReportQueryHandler(IQueuePerformanceReportService reportService)
    {
        _reportService = reportService;
    }

    public Task<QueuePerformanceReportResult> Handle(
        QueuePerformanceReportQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new QueuePerformanceFilter(
            request.OrganisationId,
            request.StartDate,
            request.EndDate,
            request.ServiceId,
            request.OfficeId);

        return _reportService.GenerateAsync(filter, cancellationToken);
    }
}

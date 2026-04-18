using MediatR;
using NextTurn.Application.Queue.Reports;

namespace NextTurn.Application.Queue.Queries.ExportQueuePerformanceReportCsv;

public sealed record ExportQueuePerformanceReportCsvQuery(
    Guid OrganisationId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ServiceId,
    Guid? OfficeId) : IRequest<QueuePerformanceCsvExportResult>;

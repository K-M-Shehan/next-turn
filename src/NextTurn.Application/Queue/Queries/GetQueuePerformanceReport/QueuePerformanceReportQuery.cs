using MediatR;
using NextTurn.Application.Queue.Reports;

namespace NextTurn.Application.Queue.Queries.GetQueuePerformanceReport;

public sealed record QueuePerformanceReportQuery(
    Guid OrganisationId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ServiceId,
    Guid? OfficeId) : IRequest<QueuePerformanceReportResult>;

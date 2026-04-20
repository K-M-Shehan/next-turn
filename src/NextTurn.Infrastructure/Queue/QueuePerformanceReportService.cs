using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Reports;
using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Infrastructure.Queue;

public sealed class QueuePerformanceReportService : IQueuePerformanceReportService
{
    private readonly IApplicationDbContext _context;
    private readonly QueuePerformanceCalculator _calculator;

    public QueuePerformanceReportService(
        IApplicationDbContext context,
        QueuePerformanceCalculator calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<QueuePerformanceReportResult> GenerateAsync(
        QueuePerformanceFilter filter,
        CancellationToken cancellationToken)
    {
        var rangeStart = filter.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEndExclusive = filter.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var baseQuery =
            from log in _context.QueueActionAuditLogs.AsNoTracking()
            join entry in _context.QueueEntries.AsNoTracking()
                on log.QueueEntryId equals entry.Id
            join queue in _context.Queues.AsNoTracking()
                on entry.QueueId equals queue.Id
            where log.ActionType == QueueActionType.Serve
                  && queue.OrganisationId == filter.OrganisationId
                  && log.CreatedAt >= rangeStart
                  && log.CreatedAt < rangeEndExclusive
            select new
            {
                log.CreatedAt,
                log.PerformedByUserId,
                entry.JoinedAt,
            };

        if (filter.OfficeId.HasValue)
        {
            var officeId = filter.OfficeId.Value;
            baseQuery = baseQuery.Where(x =>
                _context.StaffOfficeAssignments.Any(a =>
                    a.StaffUserId == x.PerformedByUserId &&
                    a.OfficeId == officeId));
        }

        if (filter.ServiceId.HasValue)
        {
            var serviceId = filter.ServiceId.Value;
            baseQuery = baseQuery.Where(x =>
                (from staffOffice in _context.StaffOfficeAssignments
                 join serviceOffice in _context.ServiceOfficeAssignments
                     on staffOffice.OfficeId equals serviceOffice.OfficeId
                 where staffOffice.StaffUserId == x.PerformedByUserId
                       && serviceOffice.ServiceId == serviceId
                 select staffOffice.OfficeId).Any());
        }

        var dataPoints = await baseQuery
            .Select(x => new QueuePerformanceDataPoint(
                x.JoinedAt,
                x.CreatedAt,
                x.CreatedAt.UtcDateTime.Hour))
            .ToListAsync(cancellationToken);

        return _calculator.Calculate(filter, dataPoints);
    }
}

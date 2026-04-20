using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.DailySummary;
using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Infrastructure.Queue;

public sealed class DailyQueueSummaryReportService : IDailyQueueSummaryReportService
{
    private readonly IApplicationDbContext _context;
    private readonly DailyQueueSummaryCalculator _calculator;

    public DailyQueueSummaryReportService(
        IApplicationDbContext context,
        DailyQueueSummaryCalculator calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<DailyQueueSummaryReportResult> GenerateAsync(
        Guid organisationId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var previousWeekDate = date.AddDays(-7);
        var rangeStart = previousWeekDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEndExclusive = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var raw = await (
            from log in _context.QueueActionAuditLogs.AsNoTracking()
            join staffOffice in _context.StaffOfficeAssignments.AsNoTracking()
                on new { log.OrganisationId, StaffUserId = log.PerformedByUserId }
                equals new { staffOffice.OrganisationId, staffOffice.StaffUserId }
            join serviceOffice in _context.ServiceOfficeAssignments.AsNoTracking()
                on new { staffOffice.OrganisationId, staffOffice.OfficeId }
                equals new { serviceOffice.OrganisationId, serviceOffice.OfficeId }
            join office in _context.Offices.AsNoTracking()
                on staffOffice.OfficeId equals office.Id
            join service in _context.Services.AsNoTracking()
                on serviceOffice.ServiceId equals service.Id
            where log.OrganisationId == organisationId
                  && log.CreatedAt >= rangeStart
                  && log.CreatedAt < rangeEndExclusive
                  && (log.ActionType == QueueActionType.Serve
                      || log.ActionType == QueueActionType.Skip
                      || log.ActionType == QueueActionType.NoShow)
            select new
            {
                log.CreatedAt,
                log.ActionType,
                OfficeId = office.Id,
                OfficeName = office.Name,
                ServiceId = service.Id,
                ServiceName = service.Name,
            })
            .ToListAsync(cancellationToken);

        var points = raw
            .GroupBy(x => new
            {
                Date = DateOnly.FromDateTime(x.CreatedAt.UtcDateTime),
                x.OfficeId,
                x.OfficeName,
                x.ServiceId,
                x.ServiceName,
                x.ActionType,
            })
            .Select(g => new DailyQueueSummaryAggregationPoint(
                Date: g.Key.Date,
                OfficeId: g.Key.OfficeId,
                OfficeName: g.Key.OfficeName,
                ServiceId: g.Key.ServiceId,
                ServiceName: g.Key.ServiceName,
                ActionType: g.Key.ActionType,
                Count: g.Count()))
            .ToList();

        return _calculator.Calculate(date, points);
    }
}

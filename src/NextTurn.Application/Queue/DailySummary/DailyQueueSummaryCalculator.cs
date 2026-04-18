using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Application.Queue.DailySummary;

public sealed class DailyQueueSummaryCalculator
{
    public DailyQueueSummaryReportResult Calculate(
        DateOnly date,
        IReadOnlyList<DailyQueueSummaryAggregationPoint> points)
    {
        var previousDayDate = date.AddDays(-1);
        var previousWeekDate = date.AddDays(-7);

        var grouped = points
            .GroupBy(x => (x.Date, x.OfficeId, x.OfficeName, x.ServiceId, x.ServiceName))
            .ToDictionary(
                g => g.Key,
                g => new MetricBucket(
                    Served: g.Where(x => x.ActionType == QueueActionType.Serve).Sum(x => x.Count),
                    Skipped: g.Where(x => x.ActionType == QueueActionType.Skip).Sum(x => x.Count),
                    NoShows: g.Where(x => x.ActionType == QueueActionType.NoShow).Sum(x => x.Count)));

        var currentRows = grouped
            .Where(x => x.Key.Date == date)
            .OrderBy(x => x.Key.OfficeName)
            .ThenBy(x => x.Key.ServiceName)
            .ToList();

        var rows = currentRows
            .Select(x =>
            {
                var previousDay = GetBucket(grouped, previousDayDate, x.Key.OfficeId, x.Key.OfficeName, x.Key.ServiceId, x.Key.ServiceName);
                var previousWeek = GetBucket(grouped, previousWeekDate, x.Key.OfficeId, x.Key.OfficeName, x.Key.ServiceId, x.Key.ServiceName);

                return new DailyQueueSummaryRow(
                    OfficeId: x.Key.OfficeId,
                    OfficeName: x.Key.OfficeName,
                    ServiceId: x.Key.ServiceId,
                    ServiceName: x.Key.ServiceName,
                    Served: x.Value.Served,
                    Skipped: x.Value.Skipped,
                    NoShows: x.Value.NoShows,
                    ServedTrend: BuildTrend(x.Value.Served, previousDay.Served, previousWeek.Served),
                    SkippedTrend: BuildTrend(x.Value.Skipped, previousDay.Skipped, previousWeek.Skipped),
                    NoShowsTrend: BuildTrend(x.Value.NoShows, previousDay.NoShows, previousWeek.NoShows));
            })
            .ToList();

        var currentTotals = Sum(rows.Select(x => new MetricBucket(x.Served, x.Skipped, x.NoShows)));

        var previousDayTotals = Sum(grouped
            .Where(x => x.Key.Date == previousDayDate)
            .Select(x => x.Value));

        var previousWeekTotals = Sum(grouped
            .Where(x => x.Key.Date == previousWeekDate)
            .Select(x => x.Value));

        return new DailyQueueSummaryReportResult(
            Date: date,
            PreviousDayDate: previousDayDate,
            PreviousWeekDate: previousWeekDate,
            TotalServed: currentTotals.Served,
            TotalSkipped: currentTotals.Skipped,
            TotalNoShows: currentTotals.NoShows,
            TotalServedTrend: BuildTrend(currentTotals.Served, previousDayTotals.Served, previousWeekTotals.Served),
            TotalSkippedTrend: BuildTrend(currentTotals.Skipped, previousDayTotals.Skipped, previousWeekTotals.Skipped),
            TotalNoShowsTrend: BuildTrend(currentTotals.NoShows, previousDayTotals.NoShows, previousWeekTotals.NoShows),
            Rows: rows);
    }

    private static MetricBucket GetBucket(
        IReadOnlyDictionary<(DateOnly Date, Guid OfficeId, string OfficeName, Guid ServiceId, string ServiceName), MetricBucket> grouped,
        DateOnly date,
        Guid officeId,
        string officeName,
        Guid serviceId,
        string serviceName)
    {
        return grouped.TryGetValue((date, officeId, officeName, serviceId, serviceName), out var bucket)
            ? bucket
            : MetricBucket.Empty;
    }

    private static MetricBucket Sum(IEnumerable<MetricBucket> buckets)
    {
        var result = MetricBucket.Empty;

        foreach (var bucket in buckets)
        {
            result = new MetricBucket(
                result.Served + bucket.Served,
                result.Skipped + bucket.Skipped,
                result.NoShows + bucket.NoShows);
        }

        return result;
    }

    private static DailyQueueMetricTrend BuildTrend(int current, int previousDay, int previousWeek)
    {
        var dayDelta = current - previousDay;
        var weekDelta = current - previousWeek;

        return new DailyQueueMetricTrend(
            PreviousDay: previousDay,
            PreviousWeek: previousWeek,
            DeltaFromPreviousDay: dayDelta,
            DeltaFromPreviousWeek: weekDelta,
            ChangePercentFromPreviousDay: CalculateChangePercent(current, previousDay),
            ChangePercentFromPreviousWeek: CalculateChangePercent(current, previousWeek));
    }

    private static double CalculateChangePercent(int current, int baseline)
    {
        if (baseline == 0)
            return current == 0 ? 0 : 100;

        return Math.Round(((double)(current - baseline) / baseline) * 100, 2);
    }

    private sealed record MetricBucket(int Served, int Skipped, int NoShows)
    {
        public static MetricBucket Empty => new(0, 0, 0);
    }
}

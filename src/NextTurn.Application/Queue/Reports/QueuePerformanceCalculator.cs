namespace NextTurn.Application.Queue.Reports;

public sealed class QueuePerformanceCalculator
{
    public QueuePerformanceReportResult Calculate(
        QueuePerformanceFilter filter,
        IReadOnlyList<QueuePerformanceDataPoint> dataPoints)
    {
        if (dataPoints.Count == 0)
        {
            return new QueuePerformanceReportResult(
                filter.StartDate,
                filter.EndDate,
                filter.ServiceId,
                filter.OfficeId,
                TotalServed: 0,
                AverageWaitMinutes: 0,
                PeakHours: Array.Empty<PeakHourSummary>());
        }

        var averageWaitMinutes = Math.Round(
            dataPoints
                .Select(p => (p.ServedAt - p.JoinedAt).TotalMinutes)
                .Select(m => m < 0 ? 0 : m)
                .Average(),
            2);

        var peakHours = dataPoints
            .GroupBy(x => x.HourOfDay)
            .Select(g => new PeakHourSummary(g.Key, g.Count()))
            .OrderByDescending(x => x.ServedCount)
            .ThenBy(x => x.HourOfDay)
            .Take(3)
            .ToList();

        return new QueuePerformanceReportResult(
            filter.StartDate,
            filter.EndDate,
            filter.ServiceId,
            filter.OfficeId,
            TotalServed: dataPoints.Count,
            AverageWaitMinutes: averageWaitMinutes,
            PeakHours: peakHours);
    }
}

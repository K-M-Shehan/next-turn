using System.Text;
using FluentAssertions;
using NextTurn.Application.Queue.Reports;
using NextTurn.Infrastructure.Queue;

namespace NextTurn.UnitTests.Infrastructure.Queue;

public sealed class QueuePerformanceExportServiceTests
{
    private readonly QueuePerformanceExportService _service = new();

    [Fact]
    public void ExportCsv_IncludesSummaryFieldsAndPeakHours()
    {
        var report = new QueuePerformanceReportResult(
            StartDate: new DateOnly(2026, 3, 1),
            EndDate: new DateOnly(2026, 3, 31),
            ServiceId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            OfficeId: null,
            TotalServed: 27,
            AverageWaitMinutes: 8.42,
            PeakHours: new[]
            {
                new PeakHourSummary(9, 12),
                new PeakHourSummary(10, 8),
            });

        var csvBytes = _service.ExportCsv(report);
        var csv = Encoding.UTF8.GetString(csvBytes);

        csv.Should().Contain("Metric,Value");
        csv.Should().Contain("StartDate,2026-03-01");
        csv.Should().Contain("EndDate,2026-03-31");
        csv.Should().Contain("TotalServed,27");
        csv.Should().Contain("AverageWaitMinutes,8.42");
        csv.Should().Contain("PeakHour,ServedCount");
        csv.Should().Contain("09:00-09:59,12");
        csv.Should().Contain("10:00-10:59,8");
    }

    [Fact]
    public void ExportCsv_WhenNoOptionalFilters_WritesAllMarker()
    {
        var report = new QueuePerformanceReportResult(
            StartDate: new DateOnly(2026, 3, 1),
            EndDate: new DateOnly(2026, 3, 31),
            ServiceId: null,
            OfficeId: null,
            TotalServed: 0,
            AverageWaitMinutes: 0,
            PeakHours: Array.Empty<PeakHourSummary>());

        var csvBytes = _service.ExportCsv(report);
        var csv = Encoding.UTF8.GetString(csvBytes);

        csv.Should().Contain("ServiceId,All");
        csv.Should().Contain("OfficeId,All");
    }
}

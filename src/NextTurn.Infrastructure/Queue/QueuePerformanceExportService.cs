using System.Text;
using NextTurn.Application.Queue.Reports;

namespace NextTurn.Infrastructure.Queue;

public sealed class QueuePerformanceExportService : IQueuePerformanceExportService
{
    public byte[] ExportCsv(QueuePerformanceReportResult report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Metric,Value");
        sb.AppendLine($"StartDate,{report.StartDate:yyyy-MM-dd}");
        sb.AppendLine($"EndDate,{report.EndDate:yyyy-MM-dd}");
        sb.AppendLine($"ServiceId,{report.ServiceId?.ToString() ?? "All"}");
        sb.AppendLine($"OfficeId,{report.OfficeId?.ToString() ?? "All"}");
        sb.AppendLine($"TotalServed,{report.TotalServed}");
        sb.AppendLine($"AverageWaitMinutes,{report.AverageWaitMinutes:F2}");
        sb.AppendLine();
        sb.AppendLine("PeakHour,ServedCount");

        foreach (var peakHour in report.PeakHours)
        {
            sb.AppendLine($"{peakHour.HourOfDay:00}:00-{peakHour.HourOfDay:00}:59,{peakHour.ServedCount}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

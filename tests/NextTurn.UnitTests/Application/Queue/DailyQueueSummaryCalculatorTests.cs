using FluentAssertions;
using NextTurn.Application.Queue.DailySummary;
using NextTurn.Domain.Queue.Enums;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class DailyQueueSummaryCalculatorTests
{
    private readonly DailyQueueSummaryCalculator _calculator = new();

    private static readonly Guid OfficeA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ServiceA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Calculate_WithNoData_ReturnsEmptySummary()
    {
        var date = new DateOnly(2026, 4, 10);

        var result = _calculator.Calculate(date, Array.Empty<DailyQueueSummaryAggregationPoint>());

        result.TotalServed.Should().Be(0);
        result.TotalSkipped.Should().Be(0);
        result.TotalNoShows.Should().Be(0);
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ComputesRowMetricsAndTrends()
    {
        var date = new DateOnly(2026, 4, 10);
        var points = new List<DailyQueueSummaryAggregationPoint>
        {
            Point(date, QueueActionType.Serve, 5),
            Point(date, QueueActionType.Skip, 2),
            Point(date, QueueActionType.NoShow, 1),

            Point(date.AddDays(-1), QueueActionType.Serve, 3),
            Point(date.AddDays(-1), QueueActionType.Skip, 1),
            Point(date.AddDays(-1), QueueActionType.NoShow, 0),

            Point(date.AddDays(-7), QueueActionType.Serve, 6),
            Point(date.AddDays(-7), QueueActionType.Skip, 0),
            Point(date.AddDays(-7), QueueActionType.NoShow, 1),
        };

        var result = _calculator.Calculate(date, points);
        var row = result.Rows.Should().ContainSingle().Subject;

        row.Served.Should().Be(5);
        row.Skipped.Should().Be(2);
        row.NoShows.Should().Be(1);

        row.ServedTrend.DeltaFromPreviousDay.Should().Be(2);
        row.ServedTrend.DeltaFromPreviousWeek.Should().Be(-1);
        row.SkippedTrend.DeltaFromPreviousDay.Should().Be(1);
        row.NoShowsTrend.DeltaFromPreviousWeek.Should().Be(0);
    }

    [Fact]
    public void Calculate_ComputesTotalTrendsAcrossRows()
    {
        var date = new DateOnly(2026, 4, 10);
        var officeB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var serviceB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var points = new List<DailyQueueSummaryAggregationPoint>
        {
            Point(date, QueueActionType.Serve, 2),
            new(date, officeB, "Office B", serviceB, "Service B", QueueActionType.Serve, 3),

            Point(date.AddDays(-1), QueueActionType.Serve, 1),
            new(date.AddDays(-1), officeB, "Office B", serviceB, "Service B", QueueActionType.Serve, 1),

            Point(date.AddDays(-7), QueueActionType.Serve, 4),
            new(date.AddDays(-7), officeB, "Office B", serviceB, "Service B", QueueActionType.Serve, 1),
        };

        var result = _calculator.Calculate(date, points);

        result.TotalServed.Should().Be(5);
        result.TotalServedTrend.DeltaFromPreviousDay.Should().Be(3);
        result.TotalServedTrend.DeltaFromPreviousWeek.Should().Be(0);
    }

    [Fact]
    public void Calculate_UsesHundredPercentWhenBaselineIsZero()
    {
        var date = new DateOnly(2026, 4, 10);
        var points = new List<DailyQueueSummaryAggregationPoint>
        {
            Point(date, QueueActionType.NoShow, 2),
        };

        var result = _calculator.Calculate(date, points);

        result.TotalNoShowsTrend.PreviousDay.Should().Be(0);
        result.TotalNoShowsTrend.ChangePercentFromPreviousDay.Should().Be(100);
    }

    private static DailyQueueSummaryAggregationPoint Point(DateOnly date, QueueActionType actionType, int count)
    {
        return new DailyQueueSummaryAggregationPoint(
            Date: date,
            OfficeId: OfficeA,
            OfficeName: "Main Office",
            ServiceId: ServiceA,
            ServiceName: "Citizen Service",
            ActionType: actionType,
            Count: count);
    }
}

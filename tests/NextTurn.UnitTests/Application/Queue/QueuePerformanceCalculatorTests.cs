using FluentAssertions;
using NextTurn.Application.Queue.Reports;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class QueuePerformanceCalculatorTests
{
    private static readonly Guid OrganisationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly QueuePerformanceCalculator _calculator = new();

    [Fact]
    public void Calculate_WithNoDataPoints_ReturnsZeroSummary()
    {
        var filter = new QueuePerformanceFilter(
            OrganisationId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            ServiceId: null,
            OfficeId: null);

        var result = _calculator.Calculate(filter, Array.Empty<QueuePerformanceDataPoint>());

        result.TotalServed.Should().Be(0);
        result.AverageWaitMinutes.Should().Be(0);
        result.PeakHours.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ComputesAverageWaitMinutes_ToTwoDecimals()
    {
        var filter = new QueuePerformanceFilter(
            OrganisationId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            ServiceId: null,
            OfficeId: null);

        var data = new[]
        {
            new QueuePerformanceDataPoint(
                new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 1, 9, 10, 0, TimeSpan.Zero),
                HourOfDay: 9),
            new QueuePerformanceDataPoint(
                new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 1, 10, 20, 0, TimeSpan.Zero),
                HourOfDay: 10),
            // Invalid negative wait values are clamped to zero.
            new QueuePerformanceDataPoint(
                new DateTimeOffset(2026, 3, 1, 11, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 1, 11, 20, 0, TimeSpan.Zero),
                HourOfDay: 11),
        };

        var result = _calculator.Calculate(filter, data);

        result.TotalServed.Should().Be(3);
        result.AverageWaitMinutes.Should().Be(10);
    }

    [Fact]
    public void Calculate_ReturnsTopThreePeakHours_OrderedByCountThenHour()
    {
        var filter = new QueuePerformanceFilter(
            OrganisationId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            ServiceId: null,
            OfficeId: null);

        var points = new List<QueuePerformanceDataPoint>();

        points.AddRange(CreatePoints(hour: 10, count: 4));
        points.AddRange(CreatePoints(hour: 14, count: 3));
        points.AddRange(CreatePoints(hour: 9, count: 3));
        points.AddRange(CreatePoints(hour: 8, count: 2));

        var result = _calculator.Calculate(filter, points);

        result.PeakHours.Should().HaveCount(3);
        result.PeakHours[0].HourOfDay.Should().Be(10);
        result.PeakHours[0].ServedCount.Should().Be(4);
        result.PeakHours[1].HourOfDay.Should().Be(9);
        result.PeakHours[1].ServedCount.Should().Be(3);
        result.PeakHours[2].HourOfDay.Should().Be(14);
        result.PeakHours[2].ServedCount.Should().Be(3);
    }

    [Fact]
    public void Calculate_PreservesRequestedFilterMetadata()
    {
        var serviceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var officeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var filter = new QueuePerformanceFilter(
            OrganisationId,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 31),
            serviceId,
            officeId);

        var result = _calculator.Calculate(filter, CreatePoints(hour: 13, count: 1));

        result.StartDate.Should().Be(new DateOnly(2026, 3, 10));
        result.EndDate.Should().Be(new DateOnly(2026, 3, 31));
        result.ServiceId.Should().Be(serviceId);
        result.OfficeId.Should().Be(officeId);
    }

    private static IReadOnlyList<QueuePerformanceDataPoint> CreatePoints(int hour, int count)
    {
        var result = new List<QueuePerformanceDataPoint>();

        for (var i = 0; i < count; i++)
        {
            var joined = new DateTimeOffset(2026, 3, 1, hour, 0, 0, TimeSpan.Zero).AddMinutes(i);
            var served = joined.AddMinutes(5);
            result.Add(new QueuePerformanceDataPoint(joined, served, hour));
        }

        return result;
    }
}

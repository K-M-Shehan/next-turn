using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Infrastructure.Persistence;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;
using ServiceOfficeAssignmentEntity = NextTurn.Domain.Service.Entities.ServiceOfficeAssignment;

namespace NextTurn.IntegrationTests.Queue;

[Collection("Integration")]
[Trait("Suite", "Regression")]
[Trait("Type", "Full")]
[Trait("Layer", "Integration")]
public sealed class DailyQueueSummaryReportIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _queueId;
    private Guid _staffUserId;

    public DailyQueueSummaryReportIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _queueId) = await _factory.SeedQueueAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var staffUser = User.Create(
            tenantId: _tenantId,
            name: "Summary Staff",
            email: new EmailAddress("summary.staff@nextturn.dev"),
            phone: null,
            passwordHash: "integration-password-hash",
            role: UserRole.Staff);
        staffUser.Activate();

        var office = OfficeEntity.Create(
            organisationId: _tenantId,
            name: "Main Office",
            address: "1 High Street",
            latitude: null,
            longitude: null,
            openingHours: "Mon-Fri 09:00-17:00");

        var service = ServiceEntity.Create(
            organisationId: _tenantId,
            name: "Citizen Service",
            code: "CIT-001",
            description: "General citizen requests",
            estimatedDurationMinutes: 15,
            isActive: true);

        db.Users.Add(staffUser);
        db.Offices.Add(office);
        db.Services.Add(service);
        db.QueueStaffAssignments.Add(QueueStaffAssignment.Create(_tenantId, _queueId, staffUser.Id));
        db.StaffOfficeAssignments.Add(StaffOfficeAssignment.Create(_tenantId, staffUser.Id, office.Id));
        db.ServiceOfficeAssignments.Add(ServiceOfficeAssignmentEntity.Create(service.Id, office.Id, _tenantId));

        await db.SaveChangesAsync();

        _staffUserId = staffUser.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDailySummaryReport_ReturnsAccurateCountsAndTrends()
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Current day: served 2, skipped 1, no-show 1.
        await AddServedActionAsync(targetDate, Guid.NewGuid());
        await AddServedActionAsync(targetDate, Guid.NewGuid());
        await AddSkippedActionAsync(targetDate, Guid.NewGuid());
        await AddNoShowActionAsync(targetDate, Guid.NewGuid());

        // Previous day: served 1, skipped 1, no-show 0.
        await AddServedActionAsync(targetDate.AddDays(-1), Guid.NewGuid());
        await AddSkippedActionAsync(targetDate.AddDays(-1), Guid.NewGuid());

        // Previous week: served 3, skipped 0, no-show 1.
        await AddServedActionAsync(targetDate.AddDays(-7), Guid.NewGuid());
        await AddServedActionAsync(targetDate.AddDays(-7), Guid.NewGuid());
        await AddServedActionAsync(targetDate.AddDays(-7), Guid.NewGuid());
        await AddNoShowActionAsync(targetDate.AddDays(-7), Guid.NewGuid());

        var response = await AdminClient().GetAsync($"/api/queues/reports/daily-summary?date={targetDate:yyyy-MM-dd}");
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        body["totalServed"].GetInt32().Should().Be(2);
        body["totalSkipped"].GetInt32().Should().Be(1);
        body["totalNoShows"].GetInt32().Should().Be(1);

        body["totalServedTrend"].GetProperty("deltaFromPreviousDay").GetInt32().Should().Be(1);
        body["totalServedTrend"].GetProperty("deltaFromPreviousWeek").GetInt32().Should().Be(-1);
        body["totalSkippedTrend"].GetProperty("deltaFromPreviousDay").GetInt32().Should().Be(0);
        body["totalNoShowsTrend"].GetProperty("deltaFromPreviousWeek").GetInt32().Should().Be(0);

        var rows = body["rows"].EnumerateArray().ToList();
        rows.Should().ContainSingle();
        rows[0].GetProperty("served").GetInt32().Should().Be(2);
        rows[0].GetProperty("skipped").GetInt32().Should().Be(1);
        rows[0].GetProperty("noShows").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetDailySummaryReport_WithUserRole_ReturnsForbidden()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await UserClient(Guid.NewGuid()).GetAsync($"/api/queues/reports/daily-summary?date={today}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task AddServedActionAsync(DateOnly actionDate, Guid userId)
    {
        await JoinQueueAsync(userId);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/serve-next", null);
        await ShiftLatestAuditLogDateAsync(actionDate);
    }

    private async Task AddSkippedActionAsync(DateOnly actionDate, Guid userId)
    {
        await JoinQueueAsync(userId);
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        await StaffClient().PostAsync($"/api/queues/{_queueId}/skip", content);
        await ShiftLatestAuditLogDateAsync(actionDate);
    }

    private async Task AddNoShowActionAsync(DateOnly actionDate, Guid userId)
    {
        await JoinQueueAsync(userId);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/no-show", null);
        await ShiftLatestAuditLogDateAsync(actionDate);
    }

    private Task<HttpResponseMessage> JoinQueueAsync(Guid userId)
    {
        return UserClient(userId).PostAsync($"/api/queues/{_queueId}/join", null);
    }

    private async Task ShiftLatestAuditLogDateAsync(DateOnly actionDate)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var latestLog = await db.QueueActionAuditLogs
            .IgnoreQueryFilters()
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync(x => x.OrganisationId == _tenantId);

        var shifted = new DateTimeOffset(
            actionDate.Year,
            actionDate.Month,
            actionDate.Day,
            10,
            0,
            0,
            TimeSpan.Zero);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE QueueActionAuditLogs SET CreatedAt = {shifted} WHERE Id = {latestLog.Id}");
    }

    private HttpClient AdminClient()
    {
        var token = _factory.CreateTokenForRole(UserRole.OrgAdmin, userId: Guid.NewGuid(), tenantId: _tenantId);
        return AuthenticatedClient(token);
    }

    private HttpClient StaffClient()
    {
        var token = _factory.CreateTokenForRole(UserRole.Staff, userId: _staffUserId, tenantId: _tenantId);
        return AuthenticatedClient(token);
    }

    private HttpClient UserClient(Guid userId)
    {
        var token = _factory.CreateTokenForRole(UserRole.User, userId: userId, tenantId: _tenantId);
        return AuthenticatedClient(token);
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        return client;
    }

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

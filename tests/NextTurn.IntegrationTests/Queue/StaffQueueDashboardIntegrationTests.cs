using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Domain.Queue.Enums;
using NextTurn.IntegrationTests;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Queue;

/// <summary>
/// Integration tests for staff queue dashboard endpoints:
/// - GET  /api/queues/{queueId}/dashboard
/// - POST /api/queues/{queueId}/call-next
/// - POST /api/queues/{queueId}/served
/// - POST /api/queues/{queueId}/no-show
/// - POST /api/queues/{queueId}/serve-next
/// - POST /api/queues/{queueId}/skip
/// </summary>
[Collection("Integration")]
[Trait("Suite", "Regression")]
[Trait("Type", "Full")]
[Trait("Layer", "Integration")]
public sealed class StaffQueueDashboardIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _queueId;
    private Guid _staffUserId;
    private Guid _userAId;
    private Guid _userBId;

    public StaffQueueDashboardIntegrationTests(NextTurnWebApplicationFactory factory)
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
            name: "Queue Staff",
            email: new EmailAddress("staff.integration@nextturn.dev"),
            phone: null,
            passwordHash: "integration-password-hash",
            role: UserRole.Staff);

        staffUser.Activate();

        db.Users.Add(staffUser);
        var userA = User.Create(
            tenantId: _tenantId,
            name: "Queue User A",
            email: new EmailAddress("queue-user-a.integration@nextturn.dev"),
            phone: null,
            passwordHash: "integration-password-hash",
            role: UserRole.User);

        var userB = User.Create(
            tenantId: _tenantId,
            name: "Queue User B",
            email: new EmailAddress("queue-user-b.integration@nextturn.dev"),
            phone: null,
            passwordHash: "integration-password-hash",
            role: UserRole.User);

        db.Users.AddRange(userA, userB);
        db.QueueStaffAssignments.Add(NextTurn.Domain.Queue.Entities.QueueStaffAssignment.Create(
            _tenantId,
            _queueId,
            staffUser.Id));

        await db.SaveChangesAsync();
        _staffUserId = staffUser.Id;
        _userAId = userA.Id;
        _userBId = userB.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboard_WithWaitingEntries_ReturnsCurrentAndWaitingData()
    {
        await JoinQueueAsync(_userAId);
        await JoinQueueAsync(_userBId);

        var response = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["waitingCount"].GetInt32().Should().Be(2);
        body["currentlyServing"].ValueKind.Should().Be(JsonValueKind.Null);

        var waiting = body["waitingEntries"].EnumerateArray().ToList();
        waiting.Should().HaveCount(2);
        waiting[0].GetProperty("ticketNumber").GetInt32().Should().Be(1);
        waiting[1].GetProperty("ticketNumber").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task CallNext_WithWaitingEntries_SetsFirstTicketAsServing()
    {
        await JoinQueueAsync(_userAId);
        await JoinQueueAsync(_userBId);

        var callResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);
        var callBody = await ReadBodyAsync(callResponse);

        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        callBody["ticketNumber"].GetInt32().Should().Be(1);
        callBody["status"].GetString().Should().Be("Serving");

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);

        dashboardBody["waitingCount"].GetInt32().Should().Be(1);
        dashboardBody["currentlyServing"].GetProperty("ticketNumber").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task MarkServed_WithServingEntry_ClearsCurrentServing()
    {
        await JoinQueueAsync(_userAId);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);

        var servedResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/served", null);
        var servedBody = await ReadBodyAsync(servedResponse);

        servedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        servedBody["status"].GetString().Should().Be("Served");

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);

        dashboardBody["currentlyServing"].ValueKind.Should().Be(JsonValueKind.Null);
        dashboardBody["waitingCount"].GetInt32().Should().Be(0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var servedEntry = await db.QueueEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == _userAId);

        servedEntry.Should().NotBeNull();
        servedEntry!.Status.Should().Be(QueueEntryStatus.Served);
    }

    [Fact]
    public async Task CallNext_ThenMarkServed_PersistsServingToServedTransitionInDatabase()
    {
        await JoinQueueAsync(_userAId);

        var callResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);
        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var servingScope = _factory.Services.CreateAsyncScope())
        {
            var db = servingScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var servingEntry = await db.QueueEntries
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == _userAId);

            servingEntry.Should().NotBeNull();
            servingEntry!.Status.Should().Be(QueueEntryStatus.Serving);
        }

        var servedResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/served", null);
        servedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var servedScope = _factory.Services.CreateAsyncScope();
        var servedDb = servedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var servedEntryAfterTransition = await servedDb.QueueEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == _userAId);

        servedEntryAfterTransition.Should().NotBeNull();
        servedEntryAfterTransition!.Status.Should().Be(QueueEntryStatus.Served);
    }

    [Fact]
    public async Task MarkNoShow_WithServingEntry_ClearsCurrentServing()
    {
        await JoinQueueAsync(_userAId);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);

        var noShowResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/no-show", null);
        var noShowBody = await ReadBodyAsync(noShowResponse);

        noShowResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        noShowBody["status"].GetString().Should().Be("NoShow");

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);

        dashboardBody["currentlyServing"].ValueKind.Should().Be(JsonValueKind.Null);
        dashboardBody["waitingCount"].GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CallNext_WithUserRole_Returns403Forbidden()
    {
        await JoinQueueAsync(_userAId);

        var response = await UserClient(_userBId).PostAsync($"/api/queues/{_queueId}/call-next", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ServeNext_WithWaitingEntries_MarksQueueHeadServed_AndWritesAuditLog()
    {
        await JoinQueueAsync(_userAId);
        await JoinQueueAsync(_userBId);

        var serveResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/serve-next", null);
        var serveBody = await ReadBodyAsync(serveResponse);

        serveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        serveBody["status"].GetString().Should().Be("Served");
        serveBody["ticketNumber"].GetInt32().Should().Be(1);

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);
        dashboardBody["waitingCount"].GetInt32().Should().Be(1);

        var waiting = dashboardBody["waitingEntries"].EnumerateArray().ToList();
        waiting.Should().HaveCount(1);
        waiting[0].GetProperty("ticketNumber").GetInt32().Should().Be(2);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var servedEntry = await db.QueueEntries
            .IgnoreQueryFilters()
            .FirstAsync(e => e.QueueId == _queueId && e.UserId == _userAId);

        servedEntry.Status.Should().Be(QueueEntryStatus.Served);

        var audit = await db.QueueActionAuditLogs
            .IgnoreQueryFilters()
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(a => a.QueueId == _queueId);

        audit.Should().NotBeNull();
        audit!.ActionType.Should().Be(QueueActionType.Serve);
        audit.PerformedByUserId.Should().Be(_staffUserId);
        audit.QueueEntryId.Should().Be(servedEntry.Id);
        audit.Reason.Should().BeNull();
    }

    [Fact]
    public async Task Skip_WithReason_MarksQueueHeadNoShow_AndWritesAuditLog()
    {
        await JoinQueueAsync(_userAId);
        await JoinQueueAsync(_userBId);

        var content = new StringContent(
            JsonSerializer.Serialize(new { reason = "Citizen did not arrive" }),
            Encoding.UTF8,
            "application/json");

        var skipResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/skip", content);
        var skipBody = await ReadBodyAsync(skipResponse);

        skipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        skipBody["status"].GetString().Should().Be("NoShow");
        skipBody["ticketNumber"].GetInt32().Should().Be(1);

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);
        dashboardBody["waitingCount"].GetInt32().Should().Be(1);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var skippedEntry = await db.QueueEntries
            .IgnoreQueryFilters()
            .FirstAsync(e => e.QueueId == _queueId && e.UserId == _userAId);

        skippedEntry.Status.Should().Be(QueueEntryStatus.NoShow);

        var audit = await db.QueueActionAuditLogs
            .IgnoreQueryFilters()
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(a => a.QueueId == _queueId);

        audit.Should().NotBeNull();
        audit!.ActionType.Should().Be(QueueActionType.Skip);
        audit.PerformedByUserId.Should().Be(_staffUserId);
        audit.QueueEntryId.Should().Be(skippedEntry.Id);
        audit.Reason.Should().Be("Citizen did not arrive");
    }

    [Fact]
    public async Task ServeNext_WhenNextUserWithinThreshold_WritesNotificationAuditLog()
    {
        await JoinQueueAsync(_userAId);
        await JoinQueueAsync(_userBId);

        var serveResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/serve-next", null);
        serveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var waitingEntryForUserB = await db.QueueEntries
            .IgnoreQueryFilters()
            .FirstAsync(e => e.QueueId == _queueId && e.UserId == _userBId);

        var notificationAudit = await db.QueueTurnNotificationAuditLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.QueueId == _queueId &&
                a.QueueEntryId == waitingEntryForUserB.Id &&
                a.UserId == _userBId &&
                a.DeliveryStatus == "Sent");

        notificationAudit.Should().NotBeNull();
        notificationAudit!.PositionInQueue.Should().Be(1);
    }

    [Fact]
    public async Task ServeNext_WhenUserPreferenceDisabled_DoesNotWriteNotificationAuditLog()
    {
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userB = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == _userBId);
            userB.SetQueueTurnApproachingNotificationsEnabled(false);
            await db.SaveChangesAsync();
        }

        await JoinQueueAsync(_userAId);
        await JoinQueueAsync(_userBId);

        var serveResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/serve-next", null);
        serveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var assertScope = _factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exists = await assertDb.QueueTurnNotificationAuditLogs
            .IgnoreQueryFilters()
            .AnyAsync(a => a.QueueId == _queueId && a.UserId == _userBId);

        exists.Should().BeFalse();
    }

    private Task<HttpResponseMessage> JoinQueueAsync(Guid userId)
    {
        return UserClient(userId).PostAsync($"/api/queues/{_queueId}/join", null);
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

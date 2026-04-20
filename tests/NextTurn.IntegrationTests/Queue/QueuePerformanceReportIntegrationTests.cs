using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
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
public sealed class QueuePerformanceReportIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _queueId;
    private Guid _staffUserId;
    private Guid _officeId;
    private Guid _serviceId;

    private static readonly Guid UserAId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid UserBId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public QueuePerformanceReportIntegrationTests(NextTurnWebApplicationFactory factory)
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
            name: "Report Staff",
            email: new EmailAddress("reports.staff@nextturn.dev"),
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
        _officeId = office.Id;
        _serviceId = service.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetQueuePerformanceReport_WithServedEntries_ReturnsAggregatedData()
    {
        await JoinQueueAsync(UserAId);
        await JoinQueueAsync(UserBId);
        await ServeNextAsync();
        await ServeNextAsync();

        var response = await AdminClient().GetAsync($"/api/queues/reports/performance?startDate={FromDate(-1)}&endDate={FromDate(1)}");
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["totalServed"].GetInt32().Should().Be(2);
        body["averageWaitMinutes"].GetDouble().Should().BeGreaterThanOrEqualTo(0);
        body["peakHours"].EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetQueuePerformanceReport_WithOfficeAndServiceFilters_ReturnsMatchingRows()
    {
        await JoinQueueAsync(UserAId);
        await ServeNextAsync();

        var url =
            $"/api/queues/reports/performance?startDate={FromDate(-1)}&endDate={FromDate(1)}&serviceId={_serviceId}&officeId={_officeId}";
        var response = await AdminClient().GetAsync(url);
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["totalServed"].GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetQueuePerformanceReport_WithUnmatchedOfficeFilter_ReturnsZeroRows()
    {
        await JoinQueueAsync(UserAId);
        await ServeNextAsync();

        var unmatchedOfficeId = Guid.NewGuid();
        var url =
            $"/api/queues/reports/performance?startDate={FromDate(-1)}&endDate={FromDate(1)}&officeId={unmatchedOfficeId}";
        var response = await AdminClient().GetAsync(url);
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["totalServed"].GetInt32().Should().Be(0);
        body["averageWaitMinutes"].GetDouble().Should().Be(0);
    }

    [Fact]
    public async Task ExportQueuePerformanceReportCsv_ReturnsCsvFile()
    {
        await JoinQueueAsync(UserAId);
        await ServeNextAsync();

        var response = await AdminClient().GetAsync($"/api/queues/reports/performance/export?startDate={FromDate(-1)}&endDate={FromDate(1)}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        content.Should().Contain("Metric,Value");
        content.Should().Contain("TotalServed,1");
    }

    [Fact]
    public async Task GetQueuePerformanceReport_WithUserRole_ReturnsForbidden()
    {
        var response = await UserClient(UserAId).GetAsync($"/api/queues/reports/performance?startDate={FromDate(-1)}&endDate={FromDate(1)}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> JoinQueueAsync(Guid userId)
    {
        return UserClient(userId).PostAsync($"/api/queues/{_queueId}/join", null);
    }

    private Task<HttpResponseMessage> ServeNextAsync()
    {
        return StaffClient().PostAsync($"/api/queues/{_queueId}/serve-next", null);
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

    private static string FromDate(int offsetDays)
    {
        return DateOnly.FromDateTime(DateTime.UtcNow.AddDays(offsetDays)).ToString("yyyy-MM-dd");
    }

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

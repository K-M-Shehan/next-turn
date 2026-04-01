using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;

namespace NextTurn.IntegrationTests.Service;

[Collection("Integration")]
public sealed class ServiceConfigurationIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;

    public ServiceConfigurationIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _) = await _factory.SeedQueueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ServiceFlow_CreateAssignUpdateDeactivateAndList_WorksForOrgAdmin()
    {
        var adminClient = AuthenticatedClient(UserRole.OrgAdmin, Guid.NewGuid(), _tenantId);

        var officeCreate = await adminClient.PostAsJsonAsync("/api/offices", new
        {
            name = "Main Office",
            address = "1 Main Street",
            latitude = 6.9271,
            longitude = 79.8612,
            openingHours = "Mon-Fri 09:00-17:00"
        });
        officeCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var office = await officeCreate.Content.ReadFromJsonAsync<OfficeDto>();
        office.Should().NotBeNull();

        var createResponse = await adminClient.PostAsJsonAsync("/api/services", new
        {
            name = "Passport Renewal",
            code = "SVC-01",
            description = "Renewal processing",
            estimatedDurationMinutes = 20,
            isActive = true
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ServiceDto>();
        created.Should().NotBeNull();
        created!.Code.Should().Be("SVC-01");

        var assignResponse = await adminClient.PostAsJsonAsync($"/api/services/{created.ServiceId}/offices", new
        {
            officeIds = new[] { office!.OfficeId }
        });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await adminClient.GetAsync("/api/services?activeOnly=true&pageNumber=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listed = await listResponse.Content.ReadFromJsonAsync<ListServicesResponse>();
        listed.Should().NotBeNull();
        listed!.Items.Should().Contain(x => x.ServiceId == created.ServiceId && x.AssignedOfficeIds.Contains(office.OfficeId));

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/services/{created.ServiceId}", new
        {
            name = "Passport Renewal Updated",
            description = "Updated description",
            estimatedDurationMinutes = 25
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ServiceDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Passport Renewal Updated");

        var removeAssignment = await adminClient.DeleteAsync($"/api/services/{created.ServiceId}/offices/{office.OfficeId}");
        removeAssignment.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deactivateResponse = await adminClient.PatchAsync($"/api/services/{created.ServiceId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activeOnlyListAfterDeactivate = await adminClient.GetAsync("/api/services?activeOnly=true&pageNumber=1&pageSize=20");
        activeOnlyListAfterDeactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeOnlyResult = await activeOnlyListAfterDeactivate.Content.ReadFromJsonAsync<ListServicesResponse>();
        activeOnlyResult.Should().NotBeNull();
        activeOnlyResult!.Items.Should().NotContain(x => x.ServiceId == created.ServiceId);
    }

    [Fact]
    public async Task CreateService_WithUserRole_ReturnsForbidden()
    {
        var userClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);

        var response = await userClient.PostAsJsonAsync("/api/services", new
        {
            name = "Passport Renewal",
            code = "SVC-01",
            description = "Renewal processing",
            estimatedDurationMinutes = 20,
            isActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListServices_WithUserRole_ReturnsOnlyActiveWhenRequested()
    {
        var adminClient = AuthenticatedClient(UserRole.OrgAdmin, Guid.NewGuid(), _tenantId);

        var activeCreate = await adminClient.PostAsJsonAsync("/api/services", new
        {
            name = "Active Service",
            code = "SVC-A",
            description = "Active",
            estimatedDurationMinutes = 10,
            isActive = true
        });
        activeCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var inactiveCreate = await adminClient.PostAsJsonAsync("/api/services", new
        {
            name = "Inactive Service",
            code = "SVC-I",
            description = "Inactive",
            estimatedDurationMinutes = 15,
            isActive = false
        });
        inactiveCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var userClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var listResponse = await userClient.GetAsync("/api/services?activeOnly=true&pageNumber=1&pageSize=20");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<ListServicesResponse>();
        list.Should().NotBeNull();
        list!.Items.Should().Contain(x => x.Code == "SVC-A");
        list.Items.Should().NotContain(x => x.Code == "SVC-I");
    }

    private HttpClient AuthenticatedClient(UserRole role, Guid userId, Guid tenantId)
    {
        var token = _factory.CreateTokenForRole(role, userId: userId, tenantId: tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private sealed record OfficeDto(Guid OfficeId, string Name);

    private sealed record ServiceDto(
        Guid ServiceId,
        string Name,
        string Code,
        string Description,
        int EstimatedDurationMinutes,
        bool IsActive,
        IReadOnlyList<Guid> AssignedOfficeIds,
        DateTimeOffset? DeactivatedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record ListServicesResponse(
        IReadOnlyList<ServiceDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount);
}

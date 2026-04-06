using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;

namespace NextTurn.IntegrationTests.Office;

[Collection("Integration")]
public sealed class OfficeCrudIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;

    public OfficeCrudIntegrationTests(NextTurnWebApplicationFactory factory)
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
    public async Task OfficeCrudFlow_CreateGetUpdateDeactivateAndFilter_WorksForOrgAdmin()
    {
        var adminClient = AuthenticatedClient(UserRole.OrgAdmin, Guid.NewGuid(), _tenantId);

        var createResponse = await adminClient.PostAsJsonAsync("/api/offices", new
        {
            name = "Main Branch",
            address = "123 Main Street",
            latitude = 6.9271,
            longitude = 79.8612,
            openingHours = "{\"mon\":\"09:00-17:00\"}"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<OfficeDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Main Branch");
        created.IsActive.Should().BeTrue();

        var getResponse = await adminClient.GetAsync($"/api/offices/{created.OfficeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getOffice = await getResponse.Content.ReadFromJsonAsync<OfficeDto>();
        getOffice.Should().NotBeNull();
        getOffice!.Address.Should().Be("123 Main Street");

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/offices/{created.OfficeId}", new
        {
            name = "Main Branch Updated",
            address = "456 Updated Avenue",
            latitude = 6.9000,
            longitude = 79.8500,
            openingHours = "Mon-Fri 08:00-16:00"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<OfficeDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Main Branch Updated");

        var activeListResponse = await adminClient.GetAsync("/api/offices?isActive=true&pageNumber=1&pageSize=20");
        activeListResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeList = await activeListResponse.Content.ReadFromJsonAsync<ListOfficesResponse>();
        activeList.Should().NotBeNull();
        activeList!.Items.Should().Contain(x => x.OfficeId == created.OfficeId);

        var deactivateResponse = await adminClient.PatchAsync($"/api/offices/{created.OfficeId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var inactiveListResponse = await adminClient.GetAsync("/api/offices?isActive=false&pageNumber=1&pageSize=20");
        inactiveListResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var inactiveList = await inactiveListResponse.Content.ReadFromJsonAsync<ListOfficesResponse>();
        inactiveList.Should().NotBeNull();
        inactiveList!.Items.Should().Contain(x => x.OfficeId == created.OfficeId && !x.IsActive);

        var activeListAfterDeactivateResponse = await adminClient.GetAsync("/api/offices?isActive=true&pageNumber=1&pageSize=20");
        activeListAfterDeactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeListAfterDeactivate = await activeListAfterDeactivateResponse.Content.ReadFromJsonAsync<ListOfficesResponse>();
        activeListAfterDeactivate.Should().NotBeNull();
        activeListAfterDeactivate!.Items.Should().NotContain(x => x.OfficeId == created.OfficeId);

        var searchResponse = await adminClient.GetAsync("/api/offices?search=Updated&pageNumber=1&pageSize=20");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searched = await searchResponse.Content.ReadFromJsonAsync<ListOfficesResponse>();
        searched.Should().NotBeNull();
        searched!.Items.Should().Contain(x => x.OfficeId == created.OfficeId);
    }

    [Fact]
    public async Task CreateOffice_WithUserRole_ReturnsForbidden()
    {
        var userClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);

        var response = await userClient.PostAsJsonAsync("/api/offices", new
        {
            name = "Main Branch",
            address = "123 Main Street",
            openingHours = "Mon-Fri 09:00-17:00"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient AuthenticatedClient(UserRole role, Guid userId, Guid tenantId)
    {
        var token = _factory.CreateTokenForRole(role, userId: userId, tenantId: tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private sealed record OfficeDto(
        Guid OfficeId,
        string Name,
        string Address,
        decimal? Latitude,
        decimal? Longitude,
        string OpeningHours,
        bool IsActive,
        DateTimeOffset? DeactivatedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record ListOfficesResponse(
        IReadOnlyList<OfficeDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount);
}

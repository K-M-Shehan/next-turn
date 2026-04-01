using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Auth;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Auth;

[Collection("Integration")]
[Trait("Suite", "Regression")]
[Trait("Type", "Full")]
[Trait("Layer", "Integration")]
public sealed class StaffManagementIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;

    public StaffManagementIntegrationTests(NextTurnWebApplicationFactory factory)
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
    public async Task StaffManagementFlow_CreateUpdateDeactivate_AndPersistOfficeAssignments()
    {
        var adminClient = AuthenticatedClient(UserRole.OrgAdmin, Guid.NewGuid(), _tenantId);

        var officeResponse = await adminClient.PostAsJsonAsync("/api/offices", new
        {
            name = "Main Office",
            address = "No. 1 Main Street",
            openingHours = "Mon-Fri 09:00-17:00"
        });

        officeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var office = await officeResponse.Content.ReadFromJsonAsync<OfficeDto>();
        office.Should().NotBeNull();

        var createStaff = await adminClient.PostAsJsonAsync("/api/staff", new
        {
            name = "Counter Agent",
            email = "counter.agent@example.com",
            phone = "0711111111",
            officeIds = new[] { office!.OfficeId },
            counterName = "Counter A",
            shiftStart = "09:00",
            shiftEnd = "17:00"
        });

        createStaff.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createStaff.Content.ReadFromJsonAsync<StaffDto>();
        created.Should().NotBeNull();
        created!.OfficeIds.Should().Contain(office.OfficeId);

        var updateStaff = await adminClient.PutAsJsonAsync($"/api/staff/{created.StaffUserId}", new
        {
            name = "Counter Agent Updated",
            phone = "0722222222",
            officeIds = Array.Empty<Guid>(),
            counterName = "Counter B",
            shiftStart = "10:00",
            shiftEnd = "18:00"
        });

        updateStaff.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateStaff.Content.ReadFromJsonAsync<StaffDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Counter Agent Updated");
        updated.OfficeIds.Should().BeEmpty();

        var deactivate = await adminClient.PatchAsync($"/api/staff/{created.StaffUserId}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listed = await adminClient.GetFromJsonAsync<ListStaffResult>("/api/staff?pageNumber=1&pageSize=20");
        listed.Should().NotBeNull();

        listed!.Items.Should().NotContain(x => x.StaffUserId == created.StaffUserId,
            "deactivation revokes staff role, so this user is no longer listed in staff results");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedUser = await db.Users
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == created.StaffUserId);

        persistedUser.IsActive.Should().BeFalse();
        persistedUser.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task StaffEndpoints_WithUserRole_ReturnForbidden()
    {
        var userClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);

        var response = await userClient.GetAsync("/api/staff?pageNumber=1&pageSize=20");
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

    private sealed record OfficeDto(Guid OfficeId, string Name);

    private sealed record StaffDto(
        Guid StaffUserId,
        string Name,
        string Email,
        string? Phone,
        bool IsActive,
        string? CounterName,
        string? ShiftStart,
        string? ShiftEnd,
        IReadOnlyList<Guid> OfficeIds,
        DateTimeOffset CreatedAt);

    private sealed record ListStaffResult(
        IReadOnlyList<StaffDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Appointment;

[Collection("Integration")]
[Trait("Suite", "Regression")]
[Trait("Type", "Full")]
[Trait("Layer", "Integration")]
public sealed class AppointmentNotificationFlowIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _appointmentProfileId;

    public AppointmentNotificationFlowIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _) = await _factory.SeedQueueAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var profile = AppointmentProfile.Create(_tenantId, "Passport Service");
        db.AppointmentProfiles.Add(profile);
        await db.SaveChangesAsync();

        _appointmentProfileId = profile.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BookAppointment_WritesBookedNotificationAuditLog()
    {
        var userId = await CreateUserAsync("Appointment User 1");
        var client = AuthenticatedClient(UserRole.User, userId, _tenantId);
        var (slotStart, slotEnd) = SlotForTomorrow(9, 0);

        var booking = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        booking.StatusCode.Should().Be(HttpStatusCode.OK);
        var booked = await booking.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        booked.Should().NotBeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var audit = await db.AppointmentNotificationAuditLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == booked!.AppointmentId &&
                a.UserId == userId &&
                a.NotificationType == "Booked");

        audit.Should().NotBeNull();
        audit!.DeliveryStatus.Should().Be("Sent");
    }

    [Fact]
    public async Task RescheduleAppointment_WritesRescheduledNotificationAuditLog()
    {
        var userId = await CreateUserAsync("Appointment User 2");
        var client = AuthenticatedClient(UserRole.User, userId, _tenantId);

        var (oldStart, oldEnd) = SlotForTomorrow(10, 0);
        var (newStart, newEnd) = SlotForTomorrow(11, 0);

        var booking = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart = oldStart,
            slotEnd = oldEnd,
        });

        booking.StatusCode.Should().Be(HttpStatusCode.OK);
        var booked = await booking.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        booked.Should().NotBeNull();

        var reschedule = await client.PutAsJsonAsync($"/api/appointments/{booked!.AppointmentId}/reschedule", new
        {
            newSlotStart = newStart,
            newSlotEnd = newEnd,
        });

        reschedule.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescheduled = await reschedule.Content.ReadFromJsonAsync<RescheduleAppointmentApiResult>();
        rescheduled.Should().NotBeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var audit = await db.AppointmentNotificationAuditLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == rescheduled!.AppointmentId &&
                a.UserId == userId &&
                a.NotificationType == "Rescheduled");

        audit.Should().NotBeNull();
        audit!.DeliveryStatus.Should().Be("Sent");
    }

    [Fact]
    public async Task CancelAppointment_WritesCancelledNotificationAuditLog()
    {
        var userId = await CreateUserAsync("Appointment User 3");
        var client = AuthenticatedClient(UserRole.User, userId, _tenantId);
        var (slotStart, slotEnd) = SlotForTomorrow(12, 0);

        var booking = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        booking.StatusCode.Should().Be(HttpStatusCode.OK);
        var booked = await booking.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        booked.Should().NotBeNull();

        var cancel = await client.PostAsync($"/api/appointments/{booked!.AppointmentId}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var audit = await db.AppointmentNotificationAuditLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == booked.AppointmentId &&
                a.UserId == userId &&
                a.NotificationType == "Cancelled");

        audit.Should().NotBeNull();
        audit!.DeliveryStatus.Should().Be("Sent");
    }

    [Fact]
    public async Task BookAppointment_WhenBookedPreferenceDisabled_DoesNotWriteAuditLog()
    {
        var userId = await CreateUserAsync("Appointment User 4");
        var client = AuthenticatedClient(UserRole.User, userId, _tenantId);

        var preferencesUpdate = await client.PutAsJsonAsync("/api/auth/appointment-notification-preferences", new
        {
            appointmentBookedNotificationsEnabled = false,
            appointmentRescheduledNotificationsEnabled = true,
            appointmentCancelledNotificationsEnabled = true,
        });

        preferencesUpdate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (slotStart, slotEnd) = SlotForTomorrow(13, 0);

        var booking = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        booking.StatusCode.Should().Be(HttpStatusCode.OK);
        var booked = await booking.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        booked.Should().NotBeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exists = await db.AppointmentNotificationAuditLogs
            .IgnoreQueryFilters()
            .AnyAsync(a =>
                a.AppointmentId == booked!.AppointmentId &&
                a.UserId == userId &&
                a.NotificationType == "Booked");

        exists.Should().BeFalse();
    }

    private HttpClient AuthenticatedClient(UserRole role, Guid userId, Guid tenantId)
    {
        var token = _factory.CreateTokenForRole(role, userId: userId, tenantId: tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private async Task<Guid> CreateUserAsync(string name)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Create(
            tenantId: _tenantId,
            name: name,
            email: new EmailAddress($"appointment-{Guid.NewGuid():N}@nextturn.dev"),
            phone: null,
            passwordHash: "integration-password-hash",
            role: UserRole.User);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static (DateTimeOffset SlotStart, DateTimeOffset SlotEnd) SlotForTomorrow(int hour, int minute)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slotStart = new DateTimeOffset(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(30);
        return (slotStart, slotEnd);
    }

    private sealed record BookAppointmentApiResult(Guid AppointmentId);

    private sealed record RescheduleAppointmentApiResult(Guid AppointmentId, DateTimeOffset SlotStart, DateTimeOffset SlotEnd);
}

using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;
using NextTurn.E2E.Playwright.Helpers;
using static Microsoft.Playwright.Assertions;

namespace NextTurn.E2E.Playwright.Journeys;

[TestFixture]
[Category("Regression")]
public sealed class Journey2AppointmentTest : BaseE2ETest
{
    /// <summary>
    /// Validates that an authenticated citizen can book an appointment, see booking confirmation with reference, and cancel it successfully.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Retry(GlobalSetup.Retries)]
    public async Task CitizenCanBookAndCancelAppointmentAsync()
    {
        var citizenEmail = $"appt_e2e_{Guid.NewGuid():N}@nextturn.com";
        var citizenPassword = $"Nt_{Guid.NewGuid():N}!A1";
        var adminEmail = GetRequiredEnvironmentVariable("TEST_ADMIN_EMAIL");
        var adminPassword = GetRequiredEnvironmentVariable("TEST_ADMIN_PASSWORD");

        var adminToken = await AuthHelper.GetBearerTokenAsync(adminEmail, adminPassword);
        var tenantId = AuthHelper.ExtractTenantIdFromJwt(adminToken);
        var appointmentProfileId = await CreateAppointmentProfileAsync(adminToken, tenantId);
        await ConfigureAlwaysOnScheduleAsync(adminToken, tenantId, appointmentProfileId);

        await RegisterGlobalCitizenAsync(citizenEmail, citizenPassword);
        var citizenToken = await AuthHelper.GetBearerTokenAsync(citizenEmail, citizenPassword);
        var appointmentId = await BookFirstAvailableAppointmentAsync(citizenToken, tenantId, appointmentProfileId);
        if (string.IsNullOrWhiteSpace(appointmentId))
        {
            throw new InconclusiveException(
            "Could not create an appointment booking for the seeded profile within the next 14 days.");
        }

        await AuthHelper.ApplyAuthToContextAsync(Context, citizenEmail, citizenPassword);
        await Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {citizenToken}",
            ["X-Tenant-Id"] = tenantId,
        });

        await Page.GotoAsync($"/appointments/{tenantId}/{appointmentProfileId}");

        var currentAppointmentCard = await WaitForFirstVisibleAsync(
            "current appointment card",
            Page.GetByTestId("current-appointment-card"),
            Page.GetByText(new Regex("Current appointment", RegexOptions.IgnoreCase)));

        await Expect(currentAppointmentCard).ToBeVisibleAsync();
        await Expect(currentAppointmentCard).ToContainTextAsync(appointmentId);

        await ClickFirstAvailableAsync(
            "open cancel modal",
            Page.GetByTestId("open-cancel-modal-btn"),
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("cancel appointment", RegexOptions.IgnoreCase) }));

        await ClickFirstAvailableAsync(
            "confirm cancellation",
            Page.GetByTestId("confirm-cancel-btn"),
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("confirm cancellation|cancelling", RegexOptions.IgnoreCase) }));

        var cancellationMessage = await WaitForFirstVisibleAsync(
            "cancellation success",
            Page.GetByText(new Regex("Appointment cancelled", RegexOptions.IgnoreCase)),
            Page.GetByRole(AriaRole.Status, new() { NameRegex = new Regex("cancelled|canceled", RegexOptions.IgnoreCase) }));

        await Expect(cancellationMessage).ToBeVisibleAsync();
    }

    private static async Task<string> CreateAppointmentProfileAsync(string adminToken, string tenantId)
    {
        var apiBaseUrl = GetRequiredEnvironmentVariable("PLAYWRIGHT_API_URL").TrimEnd('/');
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/api/appointments/profiles")
        {
            Content = JsonContent.Create(new
            {
                name = $"E2E Smoke {Guid.NewGuid():N}",
            }),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Headers.Add("X-Tenant-Id", tenantId);

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AssertionException(
                $"Unable to create appointment profile for tenant '{tenantId}'. " +
                $"Status={(int)response.StatusCode}, Body={payload}");
        }

        var created = JsonSerializer.Deserialize<AppointmentProfileDto>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (created is null || string.IsNullOrWhiteSpace(created.AppointmentProfileId))
        {
            throw new AssertionException("Create profile response did not include appointmentProfileId.");
        }

        return created.AppointmentProfileId;
    }

    private static async Task ConfigureAlwaysOnScheduleAsync(string adminToken, string tenantId, string appointmentProfileId)
    {
        var apiBaseUrl = GetRequiredEnvironmentVariable("PLAYWRIGHT_API_URL").TrimEnd('/');
        using var client = new HttpClient();

        var dayRules = Enumerable.Range(0, 7)
            .Select(day => new AppointmentDayRuleDto(
                DayOfWeek: day,
                IsEnabled: true,
                StartTime: "00:00:00",
                EndTime: "23:59:00",
                SlotDurationMinutes: 30))
            .ToArray();

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{apiBaseUrl}/api/appointments/config?appointmentProfileId={appointmentProfileId}")
        {
            Content = JsonContent.Create(new ConfigureScheduleRequest(dayRules)),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Headers.Add("X-Tenant-Id", tenantId);

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AssertionException(
                $"Unable to configure appointment schedule for tenant '{tenantId}'. " +
                $"Status={(int)response.StatusCode}, Body={payload}");
        }
    }

    private static async Task RegisterGlobalCitizenAsync(string email, string password)
    {
        var apiBaseUrl = GetRequiredEnvironmentVariable("PLAYWRIGHT_API_URL").TrimEnd('/');
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/api/auth/register-global")
        {
            Content = JsonContent.Create(new
            {
                name = "E2E Citizen",
                email,
                phone = "0771234567",
                password,
            }),
        };

        using var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var payload = await response.Content.ReadAsStringAsync();
        throw new InconclusiveException(
            $"Could not register test global citizen account. Status={(int)response.StatusCode}, Body={payload}");
    }

    private static async Task<AvailableSlotDto?> FindFirstAvailableSlotAsync(
        string citizenToken,
        string tenantId,
        string appointmentProfileId,
        IReadOnlySet<DateOnly>? excludedDates = null)
    {
        var apiBaseUrl = GetRequiredEnvironmentVariable("PLAYWRIGHT_API_URL").TrimEnd('/');
        using var client = new HttpClient();

        for (var offset = 0; offset < 14; offset++)
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(offset));
            if (excludedDates?.Contains(date) == true)
            {
                continue;
            }

            var dateParam = date.ToString("yyyy-MM-dd");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{apiBaseUrl}/api/appointments/slots?organisationId={tenantId}&appointmentProfileId={appointmentProfileId}&date={dateParam}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", citizenToken);
            request.Headers.Add("X-Tenant-Id", tenantId);

            using var response = await client.SendAsync(request);
            var payload = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var slots = JsonSerializer.Deserialize<List<AvailableSlotDto>>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? [];

            var candidate = slots.FirstOrDefault(s => !s.IsBooked);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<string?> BookFirstAvailableAppointmentAsync(
        string citizenToken,
        string tenantId,
        string appointmentProfileId)
    {
        var excludedDates = new HashSet<DateOnly>();

        while (excludedDates.Count < 14)
        {
            var slot = await FindFirstAvailableSlotAsync(citizenToken, tenantId, appointmentProfileId, excludedDates);
            if (slot is null)
            {
                return null;
            }

            var attempt = await TryBookAppointmentAsync(
                citizenToken,
                tenantId,
                appointmentProfileId,
                slot.SlotStart,
                slot.SlotEnd);

            if (attempt.Success)
            {
                return attempt.AppointmentId;
            }

            if (attempt.OnePerDay)
            {
                excludedDates.Add(DateOnly.FromDateTime(DateTimeOffset.Parse(slot.SlotStart).DateTime));
                continue;
            }

            if (attempt.SlotConflict)
            {
                continue;
            }

            throw new InconclusiveException(attempt.ErrorMessage);
        }

        return null;
    }

    private static async Task<BookAttemptResult> TryBookAppointmentAsync(
        string citizenToken,
        string tenantId,
        string appointmentProfileId,
        string slotStart,
        string slotEnd)
    {
        var apiBaseUrl = GetRequiredEnvironmentVariable("PLAYWRIGHT_API_URL").TrimEnd('/');
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/api/appointments")
        {
            Content = JsonContent.Create(new
            {
                organisationId = tenantId,
                appointmentProfileId,
                slotStart,
                slotEnd,
            }),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", citizenToken);
        request.Headers.Add("X-Tenant-Id", tenantId);

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var booked = JsonSerializer.Deserialize<BookAppointmentResultDto>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (booked is null || string.IsNullOrWhiteSpace(booked.AppointmentId))
            {
                return new BookAttemptResult(
                    Success: false,
                    AppointmentId: null,
                    OnePerDay: false,
                    SlotConflict: false,
                    ErrorMessage: "Book appointment response did not include appointmentId.");
            }

            return new BookAttemptResult(
                Success: true,
                AppointmentId: booked.AppointmentId,
                OnePerDay: false,
                SlotConflict: false,
                ErrorMessage: null);
        }

        if ((int)response.StatusCode == 400 &&
            payload.Contains("one appointment per day", StringComparison.OrdinalIgnoreCase))
        {
            return new BookAttemptResult(
                Success: false,
                AppointmentId: null,
                OnePerDay: true,
                SlotConflict: false,
                ErrorMessage: null);
        }

        if ((int)response.StatusCode == 409)
        {
            return new BookAttemptResult(
                Success: false,
                AppointmentId: null,
                OnePerDay: false,
                SlotConflict: true,
                ErrorMessage: null);
        }

        return new BookAttemptResult(
            Success: false,
            AppointmentId: null,
            OnePerDay: false,
            SlotConflict: false,
            ErrorMessage: $"Could not create appointment booking for smoke test. Status={(int)response.StatusCode}, Body={payload}");
    }

    private sealed record ConfigureScheduleRequest(IReadOnlyList<AppointmentDayRuleDto> DayRules);

    private sealed record AppointmentDayRuleDto(
        int DayOfWeek,
        bool IsEnabled,
        string StartTime,
        string EndTime,
        int SlotDurationMinutes);

    private sealed record AppointmentProfileDto(string AppointmentProfileId, string Name, bool IsActive, string ShareableLink);

    private sealed record AvailableSlotDto(string SlotStart, string SlotEnd, bool IsBooked);

    private sealed record BookAppointmentResultDto(string AppointmentId);

    private sealed record BookAttemptResult(
        bool Success,
        string? AppointmentId,
        bool OnePerDay,
        bool SlotConflict,
        string? ErrorMessage);
}

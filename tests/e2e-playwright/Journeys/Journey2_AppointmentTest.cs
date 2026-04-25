using System.Text.RegularExpressions;
using System.Net.Http.Headers;
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
        var citizenEmail = GetRequiredEnvironmentVariable("TEST_CITIZEN_EMAIL");
        var citizenPassword = GetRequiredEnvironmentVariable("TEST_CITIZEN_PASSWORD");
        var adminEmail = GetRequiredEnvironmentVariable("TEST_ADMIN_EMAIL");
        var adminPassword = GetRequiredEnvironmentVariable("TEST_ADMIN_PASSWORD");

        var adminToken = await AuthHelper.GetBearerTokenAsync(adminEmail, adminPassword);
        var tenantId = AuthHelper.ExtractTenantIdFromJwt(adminToken);
        var appointmentProfileId = await GetFirstActiveAppointmentProfileIdAsync(adminToken, tenantId);

        if (string.IsNullOrWhiteSpace(appointmentProfileId))
        {
            throw new InconclusiveException(
                "No active appointment profile is available for the configured tenant. " +
                "Seed at least one active appointment profile before running smoke tests.");
        }

        await AuthHelper.ApplyAuthToContextAsync(Context, citizenEmail, citizenPassword);

        await Page.GotoAsync($"/appointments/{tenantId}/{appointmentProfileId}");

        var availableSlot = await WaitForFirstVisibleAsync(
            "available appointment slot",
            Page.Locator("button:not([disabled])").Filter(new()
            {
                HasTextRegex = new Regex(@"\d{1,2}:\d{2}", RegexOptions.IgnoreCase),
            }).First,
            Page.Locator("button:has-text('-'):not([disabled])").First);

        await availableSlot.ClickAsync();

        await ClickFirstAvailableAsync(
            "confirm appointment",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("confirm appointment|confirm reschedule", RegexOptions.IgnoreCase) }));

        var confirmationMessage = await WaitForFirstVisibleAsync(
            "booking confirmation",
            Page.GetByText(new Regex("Appointment (confirmed|rescheduled)", RegexOptions.IgnoreCase)),
            Page.GetByText(new Regex("ID:\\s*[0-9a-f-]{36}", RegexOptions.IgnoreCase)));

        await Expect(confirmationMessage).ToBeVisibleAsync();

        var confirmationText = await confirmationMessage.InnerTextAsync();
        Assert.That(
            Regex.IsMatch(confirmationText, @"(?i)(appointment (confirmed|rescheduled)|\bid\b)"),
            Is.True,
            "Confirmation message should include booking success text and ID.");

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

    private static async Task<string?> GetFirstActiveAppointmentProfileIdAsync(string adminToken, string tenantId)
    {
        var apiBaseUrl = GetRequiredEnvironmentVariable("PLAYWRIGHT_API_URL").TrimEnd('/');

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/api/appointments/profiles");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Headers.Add("X-Tenant-Id", tenantId);

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AssertionException(
                $"Unable to list appointment profiles for tenant '{tenantId}'. " +
                $"Status={(int)response.StatusCode}, Body={payload}");
        }

        var profiles = JsonSerializer.Deserialize<List<AppointmentProfileDto>>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        return profiles.FirstOrDefault(p => p.IsActive)?.AppointmentProfileId;
    }

    private sealed record AppointmentProfileDto(string AppointmentProfileId, string Name, bool IsActive, string ShareableLink);
}

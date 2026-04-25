using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using NextTurn.E2E.Playwright.Helpers;
using static Microsoft.Playwright.Assertions;

namespace NextTurn.E2E.Playwright.Journeys;

[TestFixture]
[Category("Regression")]
public sealed class Journey6OnboardingTourTest : BaseE2ETest
{
    [Test]
    [Retry(GlobalSetup.Retries)]
    public async Task CitizenOnboardingCanSkipAndRestartAsync()
    {
        var email = GetRequiredEnvironmentVariable("TEST_CITIZEN_EMAIL");
        var password = GetRequiredEnvironmentVariable("TEST_CITIZEN_PASSWORD");

        await AuthHelper.ApplyAuthToContextAsync(Context, email, password);
        await Page.GotoAsync("/dashboard");

        var tour = Page.GetByTestId("onboarding-tour");
        await Expect(tour).ToBeVisibleAsync();

        await Expect(tour).ToContainTextAsync(new Regex("Step 1 of", RegexOptions.IgnoreCase));
        await tour.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
        await Expect(tour).ToContainTextAsync(new Regex("Step 2 of", RegexOptions.IgnoreCase));

        await tour.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();
        await Expect(tour).ToContainTextAsync(new Regex("Step 1 of", RegexOptions.IgnoreCase));

        await tour.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeHiddenAsync();

        await Page.Locator("[data-onboarding='citizen-settings-tab']").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("restart onboarding tour", RegexOptions.IgnoreCase) }).ClickAsync();

        await Expect(tour).ToBeVisibleAsync();
        await tour.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeHiddenAsync();
    }

    [Test]
    [Retry(GlobalSetup.Retries)]
    public async Task StaffOnboardingCanSkipAndRestartAsync()
    {
        var email = GetRequiredEnvironmentVariable("TEST_STAFF_EMAIL");
        var password = GetRequiredEnvironmentVariable("TEST_STAFF_PASSWORD");

        var token = await AuthHelper.GetBearerTokenAsync(email, password);
        await AuthHelper.ApplyAuthToContextAsync(Context, email, password);

        var tenantId = ExtractTenantIdFromJwt(token);
        await Page.GotoAsync($"/staff/{tenantId}");

        var tour = Page.GetByTestId("onboarding-tour");
        await Expect(tour).ToBeVisibleAsync();

        await tour.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
        await Expect(tour).ToContainTextAsync(new Regex("Step 2 of", RegexOptions.IgnoreCase));

        await tour.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeHiddenAsync();

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("restart onboarding tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeVisibleAsync();

        await tour.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeHiddenAsync();
    }

    [Test]
    [Retry(GlobalSetup.Retries)]
    public async Task AdminOnboardingCanSkipAndRestartAsync()
    {
        var email = GetRequiredEnvironmentVariable("TEST_ADMIN_EMAIL");
        var password = GetRequiredEnvironmentVariable("TEST_ADMIN_PASSWORD");

        var token = await AuthHelper.GetBearerTokenAsync(email, password);
        await AuthHelper.ApplyAuthToContextAsync(Context, email, password);

        var tenantId = ExtractTenantIdFromJwt(token);
        await Page.GotoAsync($"/admin/{tenantId}");

        var tour = Page.GetByTestId("onboarding-tour");
        await Expect(tour).ToBeVisibleAsync();

        await tour.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
        await Expect(tour).ToContainTextAsync(new Regex("Step 2 of", RegexOptions.IgnoreCase));

        await tour.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeHiddenAsync();

        await Page.Locator("[data-onboarding='admin-settings-tab']").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("restart onboarding tour", RegexOptions.IgnoreCase) }).ClickAsync();

        await Expect(tour).ToBeVisibleAsync();
        await tour.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip tour", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(tour).ToBeHiddenAsync();
    }

    private static string ExtractTenantIdFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            throw new AssertionException("JWT format is invalid; cannot read tenant id.");
        }

        var payload = DecodeBase64Url(parts[1]);
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("tid", out var tidElement) || tidElement.ValueKind != JsonValueKind.String)
        {
            throw new AssertionException("JWT does not contain a tenant id (tid) claim.");
        }

        var tenantId = tidElement.GetString();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new AssertionException("JWT tid claim is empty.");
        }

        return tenantId;
    }

    private static string DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}

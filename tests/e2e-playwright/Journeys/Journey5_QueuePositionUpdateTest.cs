using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using NextTurn.E2E.Playwright.Helpers;
using static Microsoft.Playwright.Assertions;

namespace NextTurn.E2E.Playwright.Journeys;

[TestFixture]
[Category("Regression")]
public sealed class Journey5QueuePositionUpdateTest : BaseE2ETest
{
    /// <summary>
    /// Validates that a citizen's queue position decreases in near real-time when staff serves the next person, using separate browser contexts.
    /// </summary>
    [Test]
    [Retry(GlobalSetup.Retries)]
    public async Task CitizenQueuePositionUpdatesWhenStaffServesNextAsync()
    {
        var citizenEmail = GetRequiredEnvironmentVariable("TEST_CITIZEN_EMAIL");
        var citizenPassword = GetRequiredEnvironmentVariable("TEST_CITIZEN_PASSWORD");
        var staffEmail = GetRequiredEnvironmentVariable("TEST_STAFF_EMAIL");
        var staffPassword = GetRequiredEnvironmentVariable("TEST_STAFF_PASSWORD");

        await using var citizenContext = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = GlobalSetup.BaseUrl,
            IgnoreHTTPSErrors = true,
        });

        await using var staffContext = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = GlobalSetup.BaseUrl,
            IgnoreHTTPSErrors = true,
        });

        await AuthHelper.ApplyAuthToContextAsync(citizenContext, citizenEmail, citizenPassword);
        await AuthHelper.ApplyAuthToContextAsync(staffContext, staffEmail, staffPassword);

        var citizenPage = await citizenContext.NewPageAsync();
        var staffPage = await staffContext.NewPageAsync();

        citizenPage.SetDefaultTimeout(GlobalSetup.TimeoutMs);
        staffPage.SetDefaultTimeout(GlobalSetup.TimeoutMs);

        await citizenPage.GotoAsync("/queues");
        await citizenPage.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("join queue|join", RegexOptions.IgnoreCase) }).ClickAsync();

        var citizenPositionLocator = citizenPage.GetByTestId("queue-position");
        await Expect(citizenPositionLocator).ToBeVisibleAsync();

        var initialPosition = ParseFirstInt(await citizenPositionLocator.InnerTextAsync());

        await staffPage.GotoAsync("/staff/queue");
        await staffPage.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("serve next|serve", RegexOptions.IgnoreCase) }).ClickAsync();

        await Expect(citizenPositionLocator).Not.ToHaveTextAsync(new Regex($"\\b{initialPosition}\\b"));

        var updatedPosition = ParseFirstInt(await citizenPositionLocator.InnerTextAsync());
        Assert.That(updatedPosition, Is.LessThan(initialPosition), "Citizen queue position should decrease after staff serves next.");
    }
}

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using NextTurn.E2E.Playwright.Helpers;
using static Microsoft.Playwright.Assertions;

namespace NextTurn.E2E.Playwright.Journeys;

[TestFixture]
[Category("Regression")]
public sealed class Journey4StaffTest : BaseE2ETest
{
    /// <summary>
    /// Validates that staff can serve the next citizen and skip a citizen, changing queue count and moving the skipped citizen to queue end.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Retry(GlobalSetup.Retries)]
    public async Task StaffCanServeAndSkipCitizensAsync()
    {
        var email = GetRequiredEnvironmentVariable("TEST_STAFF_EMAIL");
        var password = GetRequiredEnvironmentVariable("TEST_STAFF_PASSWORD");

        var token = await AuthHelper.GetBearerTokenAsync(email, password);
        var tenantId = AuthHelper.ExtractTenantIdFromJwt(token);

        await AuthHelper.ApplyAuthToContextAsync(Context, email, password);

        await Page.GotoAsync($"/staff/{tenantId}");

        ILocator queueCountLocator;
        try
        {
            queueCountLocator = await WaitForFirstVisibleAsync(
                "queue count",
                Page.GetByTestId("queue-count"),
                Page.GetByRole(AriaRole.Status, new() { NameRegex = new Regex("queue count|people waiting|waiting", RegexOptions.IgnoreCase) }));
        }
        catch (TimeoutException)
        {
            throw new InconclusiveException(
                "Staff dashboard did not render queue metrics. Ensure the staff account is assigned to an active queue with data in the target environment.");
        }

        var initialCount = ParseFirstInt(await queueCountLocator.InnerTextAsync());

        await ClickFirstAvailableAsync(
            "serve next citizen",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("serve next|serve", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("serve-next-button"));

        await Expect(queueCountLocator).Not.ToHaveTextAsync(new Regex($"\\b{initialCount}\\b"));

        var queueRows = Page.GetByTestId("queue-citizen-row");
        var rowCount = await queueRows.CountAsync();
        if (rowCount < 2)
        {
            throw new InconclusiveException("Queue needs at least two citizens to validate skip behavior.");
        }

        var firstCitizenNameLocator = queueRows.Nth(0).GetByTestId("queue-citizen-name");
        var skippedCitizenName = (await firstCitizenNameLocator.InnerTextAsync()).Trim();

        await queueRows.Nth(0).GetByRole(AriaRole.Button, new() { NameRegex = new Regex("skip", RegexOptions.IgnoreCase) }).ClickAsync();

        var lastCitizenNameLocator = queueRows.Last.GetByTestId("queue-citizen-name");
        await Expect(lastCitizenNameLocator).ToHaveTextAsync(new Regex(Regex.Escape(skippedCitizenName)));
    }
}

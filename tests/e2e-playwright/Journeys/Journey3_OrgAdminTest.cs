using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using NextTurn.E2E.Playwright.Helpers;
using static Microsoft.Playwright.Assertions;

namespace NextTurn.E2E.Playwright.Journeys;

[TestFixture]
public sealed class Journey3OrgAdminTest : BaseE2ETest
{
    /// <summary>
    /// Validates that an org admin can create an office and create a service under that office, and both appear in their lists.
    /// </summary>
    [Test]
    [Retry(GlobalSetup.Retries)]
    public async Task OrgAdminCanCreateOfficeAndServiceAsync()
    {
        var email = GetRequiredEnvironmentVariable("TEST_ADMIN_EMAIL");
        var password = GetRequiredEnvironmentVariable("TEST_ADMIN_PASSWORD");

        await AuthHelper.ApplyAuthToContextAsync(Context, email, password);

        var officeName = $"E2E Office {Guid.NewGuid():N}";
        var serviceName = $"E2E Service {Guid.NewGuid():N}";

        await Page.GotoAsync("/admin/offices");

        await FillFirstAvailableAsync(
            "office name",
            officeName,
            Page.GetByLabel("Office Name", new() { Exact = false }),
            Page.GetByTestId("office-name-input"));

        await ClickFirstAvailableAsync(
            "create office",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("create office|add office|save office", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("create-office-button"));

        var officeRow = await WaitForFirstVisibleAsync(
            "created office",
            Page.GetByRole(AriaRole.Row, new() { NameRegex = new Regex(Regex.Escape(officeName), RegexOptions.IgnoreCase) }),
            Page.GetByTestId("office-list-item").Filter(new() { HasTextString = officeName }),
            Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(Regex.Escape(officeName), RegexOptions.IgnoreCase) }));

        await Expect(officeRow).ToBeVisibleAsync();

        await ClickFirstAvailableAsync(
            "open office details",
            Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(Regex.Escape(officeName), RegexOptions.IgnoreCase) }),
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(officeName), RegexOptions.IgnoreCase) }));

        await FillFirstAvailableAsync(
            "service name",
            serviceName,
            Page.GetByLabel("Service Name", new() { Exact = false }),
            Page.GetByTestId("service-name-input"));

        await ClickFirstAvailableAsync(
            "create service",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("create service|add service|save service", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("create-service-button"));

        var serviceRow = await WaitForFirstVisibleAsync(
            "created service",
            Page.GetByRole(AriaRole.Row, new() { NameRegex = new Regex(Regex.Escape(serviceName), RegexOptions.IgnoreCase) }),
            Page.GetByTestId("service-list-item").Filter(new() { HasTextString = serviceName }),
            Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(Regex.Escape(serviceName), RegexOptions.IgnoreCase) }));

        await Expect(serviceRow).ToBeVisibleAsync();
    }
}

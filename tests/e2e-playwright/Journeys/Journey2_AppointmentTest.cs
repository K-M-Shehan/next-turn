using System.Text.RegularExpressions;
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
        var email = GetRequiredEnvironmentVariable("TEST_CITIZEN_EMAIL");
        var password = GetRequiredEnvironmentVariable("TEST_CITIZEN_PASSWORD");

        await AuthHelper.ApplyAuthToContextAsync(Context, email, password);

        await Page.GotoAsync("/appointments/new");

        await FillFirstAvailableAsync(
            "appointment reason",
            $"E2E booking {Guid.NewGuid():N}",
            Page.GetByLabel("Reason", new() { Exact = false }),
            Page.GetByTestId("appointment-reason-input"));

        await ClickFirstAvailableAsync(
            "book appointment",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("book appointment|confirm booking|book", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("book-appointment-button"));

        var confirmationMessage = await WaitForFirstVisibleAsync(
            "booking confirmation",
            Page.GetByTestId("appointment-confirmation-message"),
            Page.GetByRole(AriaRole.Status, new() { NameRegex = new Regex("confirmation|booking", RegexOptions.IgnoreCase) }),
            Page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("confirmed|confirmation", RegexOptions.IgnoreCase) }));

        await Expect(confirmationMessage).ToBeVisibleAsync();

        var confirmationText = await confirmationMessage.InnerTextAsync();
        Assert.That(
            Regex.IsMatch(confirmationText, @"(?i)(reference|booking ref|appointment ref)"),
            Is.True,
            "Confirmation message should include a booking reference.");

        await ClickFirstAvailableAsync(
            "cancel appointment",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("cancel appointment|cancel", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("cancel-appointment-button"));

        var cancellationMessage = await WaitForFirstVisibleAsync(
            "cancellation success",
            Page.GetByTestId("appointment-cancel-success"),
            Page.GetByRole(AriaRole.Status, new() { NameRegex = new Regex("cancelled|canceled|success", RegexOptions.IgnoreCase) }),
            Page.GetByRole(AriaRole.Alert, new() { NameRegex = new Regex("cancelled|canceled|success", RegexOptions.IgnoreCase) }));

        await Expect(cancellationMessage).ToBeVisibleAsync();
    }
}

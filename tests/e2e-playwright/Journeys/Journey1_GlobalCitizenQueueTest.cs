using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace NextTurn.E2E.Playwright.Journeys;

[TestFixture]
public sealed class Journey1GlobalCitizenQueueTest : BaseE2ETest
{
    /// <summary>
    /// Validates that a global citizen can register, log in, join a queue, see a numeric position, and leave the queue.
    /// </summary>
    [Test]
    [Retry(GlobalSetup.Retries)]
    public async Task GlobalCitizenCanRegisterLoginJoinAndLeaveQueueAsync()
    {
        var email = $"testuser_{Guid.NewGuid():N}@nextturn.com";
        var password = Environment.GetEnvironmentVariable("TEST_CITIZEN_PASSWORD")
            ?? $"Nt_{Guid.NewGuid():N}!A1";

        await Page.GotoAsync("/register");

        await FillFirstAvailableAsync(
            "registration full name",
            "E2E Global Citizen",
            Page.GetByLabel("Full Name", new() { Exact = false }),
            Page.GetByTestId("register-name-input"));

        await FillFirstAvailableAsync(
            "registration email",
            email,
            Page.GetByLabel("Email", new() { Exact = false }),
            Page.GetByTestId("register-email-input"));

        await FillFirstAvailableAsync(
            "registration password",
            password,
            Page.GetByLabel("Password", new() { Exact = false }).First,
            Page.GetByTestId("register-password-input"));

        await FillFirstAvailableAsync(
            "registration password confirmation",
            password,
            Page.GetByLabel("Confirm Password", new() { Exact = false }),
            Page.GetByTestId("register-confirm-password-input"));

        await ClickFirstAvailableAsync(
            "register account",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("register|create account|sign up", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("register-submit-button"));

        await Page.GotoAsync("/login");

        await FillFirstAvailableAsync(
            "login email",
            email,
            Page.GetByLabel("Email", new() { Exact = false }),
            Page.GetByTestId("login-email-input"));

        await ClickFirstAvailableAsync(
            "continue to password step",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("continue", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("login-continue-button"));

        await FillFirstAvailableAsync(
            "login password",
            password,
            Page.GetByLabel("Password", new() { Exact = false }),
            Page.GetByPlaceholder("Your password"),
            Page.GetByTestId("login-password-input"));

        await ClickFirstAvailableAsync(
            "sign in",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("sign in|log in|login", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("login-submit-button"));

        await Page.GotoAsync("/queues");

        await ClickFirstAvailableAsync(
            "join queue",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("join queue|join", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("join-queue-button"));

        var queuePositionLocator = await WaitForFirstVisibleAsync(
            "queue position",
            Page.GetByTestId("queue-position"),
            Page.GetByRole(AriaRole.Status, new() { NameRegex = new Regex("queue position|position", RegexOptions.IgnoreCase) }),
            Page.GetByLabel("Queue Position", new() { Exact = false }));

        await Expect(queuePositionLocator).ToBeVisibleAsync();

        var queuePositionText = await queuePositionLocator.InnerTextAsync();
        Assert.That(
            Regex.IsMatch(queuePositionText, @"\d+"),
            Is.True,
            "Queue position should contain a numeric value.");

        await ClickFirstAvailableAsync(
            "leave queue",
            Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("leave queue|leave", RegexOptions.IgnoreCase) }),
            Page.GetByTestId("leave-queue-button"));
    }
}

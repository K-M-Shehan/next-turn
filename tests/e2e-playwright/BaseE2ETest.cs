using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NextTurn.E2E.Playwright;

public abstract class BaseE2ETest
{
    private IPlaywright? _playwright;

    protected IBrowser Browser { get; private set; } = null!;

    protected IBrowserContext Context { get; private set; } = null!;

    protected IPage Page { get; private set; } = null!;

    [SetUp]
    public async Task BeforeEachAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = GlobalSetup.Headless,
        });

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = GlobalSetup.BaseUrl,
            IgnoreHTTPSErrors = true,
        });

        Page = await Context.NewPageAsync();
        Page.SetDefaultTimeout(GlobalSetup.TimeoutMs);
        Page.SetDefaultNavigationTimeout(GlobalSetup.TimeoutMs);

        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task AfterEachAsync()
    {
        try
        {
            var result = TestContext.CurrentContext.Result.Outcome.Status;
            var isFailed = result is TestStatus.Failed or TestStatus.Inconclusive;

            var sanitizedName = SanitizeFileName(
                $"{TestContext.CurrentContext.Test.ClassName}_{TestContext.CurrentContext.Test.MethodName}_{DateTime.UtcNow:yyyyMMddHHmmss}");

            var screenshotsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-results", "screenshots");
            var tracesDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-results", "traces");

            Directory.CreateDirectory(screenshotsDir);
            Directory.CreateDirectory(tracesDir);

            if (isFailed)
            {
                await Page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(screenshotsDir, $"{sanitizedName}.png"),
                    FullPage = true,
                });

                await Context.Tracing.StopAsync(new()
                {
                    Path = Path.Combine(tracesDir, $"{sanitizedName}.zip"),
                });
            }
            else
            {
                await Context.Tracing.StopAsync();
            }
        }
        finally
        {
            if (Context is not null)
            {
                await Context.CloseAsync();
            }

            if (Browser is not null)
            {
                await Browser.CloseAsync();
            }

            _playwright?.Dispose();
        }
    }

    protected static string GetRequiredEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InconclusiveException(
            $"Environment variable '{variableName}' is required for this journey test.");
    }

    protected async Task ClickFirstAvailableAsync(string actionName, params ILocator[] candidates)
    {
        var locator = await WaitForFirstVisibleAsync(actionName, candidates);
        await locator.ClickAsync();
    }

    protected async Task FillFirstAvailableAsync(string fieldName, string value, params ILocator[] candidates)
    {
        var locator = await WaitForFirstVisibleAsync(fieldName, candidates);
        await locator.FillAsync(value);
    }

    protected async Task<ILocator> WaitForFirstVisibleAsync(string locatorName, params ILocator[] candidates)
    {
        foreach (var candidate in candidates)
        {
            try
            {
                await candidate.First.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 15000,
                });

                return candidate.First;
            }
            catch (PlaywrightException)
            {
                // Try next candidate.
            }
        }

        throw new AssertionException($"Could not find a visible locator for '{locatorName}'.");
    }

    protected static int ParseFirstInt(string value)
    {
        var match = Regex.Match(value, @"\d+");
        if (!match.Success)
        {
            throw new AssertionException($"Expected numeric value but received '{value}'.");
        }

        return int.Parse(match.Value);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value;
    }
}

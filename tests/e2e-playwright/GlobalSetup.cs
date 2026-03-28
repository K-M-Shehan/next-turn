using NUnit.Framework;

namespace NextTurn.E2E.Playwright;

[SetUpFixture]
public sealed class GlobalSetup
{
    public const int Retries = 2;
    public const float TimeoutMs = 30000;

    public static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")?.TrimEnd('/')
        ?? "http://localhost:5173";

    public static readonly bool Headless =
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    [OneTimeSetUp]
    public void Initialize()
    {
        Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-results", "screenshots"));
        Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-results", "traces"));

        TestContext.Progress.WriteLine($"Playwright Base URL: {BaseUrl}");
        TestContext.Progress.WriteLine($"Playwright Headless: {Headless}");
        TestContext.Progress.WriteLine($"Retries per test: {Retries}");
        TestContext.Progress.WriteLine($"Default timeout (ms): {TimeoutMs}");
    }
}

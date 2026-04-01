using NUnit.Framework;

namespace NextTurn.E2E.Playwright;

[SetUpFixture]
public sealed class GlobalSetup
{
    public const int Retries = 2;
    public const float TimeoutMs = 30000;
    public static bool IsBaseUrlReachable { get; private set; }
    public static string? BaseUrlProbeError { get; private set; }

    public static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")?.TrimEnd('/')
        ?? "http://localhost:5173";

    public static readonly bool Headless =
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    [OneTimeSetUp]
    public async Task Initialize()
    {
        Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-results", "screenshots"));
        Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-results", "traces"));

        await ProbeBaseUrlAsync();

        TestContext.Progress.WriteLine($"Playwright Base URL: {BaseUrl}");
        TestContext.Progress.WriteLine($"Playwright Headless: {Headless}");
        TestContext.Progress.WriteLine($"Retries per test: {Retries}");
        TestContext.Progress.WriteLine($"Default timeout (ms): {TimeoutMs}");
        TestContext.Progress.WriteLine($"Playwright Base URL reachable: {IsBaseUrlReachable}");
        if (!IsBaseUrlReachable && !string.IsNullOrWhiteSpace(BaseUrlProbeError))
        {
            TestContext.Progress.WriteLine($"Base URL probe error: {BaseUrlProbeError}");
        }
    }

    private static async Task ProbeBaseUrlAsync()
    {
        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5),
            };

            using var response = await httpClient.GetAsync(BaseUrl);
            IsBaseUrlReachable = (int)response.StatusCode < 500;
            BaseUrlProbeError = IsBaseUrlReachable
                ? null
                : $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
        }
        catch (Exception ex)
        {
            IsBaseUrlReachable = false;
            BaseUrlProbeError = ex.Message;
        }
    }
}

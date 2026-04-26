using System.Collections.Concurrent;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace NextTurn.E2E.Playwright.Helpers;

public static class AuthHelper
{
    private static readonly ConcurrentDictionary<string, string> TokenCache = new();

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")?.TrimEnd('/')
        ?? "http://localhost:5173";

    private static readonly string ApiBaseUrl =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_API_URL")?.TrimEnd('/')
        ?? BaseUrl;

    private static readonly string? TenantId =
        Environment.GetEnvironmentVariable("PLAYWRIGHT_TENANT_ID")
        ?? Environment.GetEnvironmentVariable("TEST_TENANT_ID");

    public static async Task<string> GetBearerTokenAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{username}:{password}";
        if (TokenCache.TryGetValue(cacheKey, out var cachedToken))
        {
            return cachedToken;
        }

        var requestBody = new
        {
            email = username,
            password,
        };

        var (statusCode, content) = await SendLoginAsync("/api/auth/login", requestBody, includeTenantHeader: true, cancellationToken);

        // Some citizen test accounts are global-only; fallback to login-global in that case.
        if ((int)statusCode == 400 && content.Contains("Invalid credentials.", StringComparison.OrdinalIgnoreCase))
        {
            (statusCode, content) = await SendLoginAsync("/api/auth/login-global", requestBody, includeTenantHeader: false, cancellationToken);
        }

        if ((int)statusCode == 400 &&
            content.Contains("X-Tenant-Id", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(TenantId))
        {
            throw new AssertionException(
                "Authentication failed because tenant context is missing. Set PLAYWRIGHT_TENANT_ID (or TEST_TENANT_ID) in CI secrets/environment.");
        }

        if ((int)statusCode < 200 || (int)statusCode >= 300)
        {
            throw new AssertionException(
                $"Authentication failed with status code {(int)statusCode}. Body: {content}");
        }

        var token = ExtractToken(content);
        TokenCache[cacheKey] = token;
        return token;
    }

    public static string ExtractTenantIdFromJwt(string jwt)
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

    private static async Task<(int StatusCode, string Content)> SendLoginAsync(
        string path,
        object requestBody,
        bool includeTenantHeader,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(ApiBaseUrl), path))
        {
            Content = JsonContent.Create(requestBody),
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (includeTenantHeader && !string.IsNullOrWhiteSpace(TenantId))
        {
            request.Headers.Add("X-Tenant-Id", TenantId);
        }

        using var response = await Client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ((int)response.StatusCode, content);
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

    public static async Task ApplyAuthToContextAsync(IBrowserContext context, string username, string password, CancellationToken cancellationToken = default)
    {
        var token = await GetBearerTokenAsync(username, password, cancellationToken);
        // Escape the token for use in JavaScript by wrapping in quotes
        var escapedToken = token.Replace("\\", "\\\\").Replace("'", "\\'");

        await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {token}",
        });

        await context.AddInitScriptAsync(
            $"window.localStorage.setItem('nt_access_token', '{escapedToken}');" +
            $"window.localStorage.setItem('token', '{escapedToken}');" +
            $"window.localStorage.setItem('accessToken', '{escapedToken}');" +
            $"window.localStorage.setItem('authToken', '{escapedToken}');");
    }

    private static string ExtractToken(string content)
    {
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        var candidateProperties = new[]
        {
            "token",
            "accessToken",
            "bearerToken",
            "jwt",
        };

        foreach (var property in candidateProperties)
        {
            if (!root.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var raw = element.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? raw["Bearer ".Length..].Trim()
                : raw.Trim();
        }

        throw new AssertionException("Login response does not contain a recognized bearer token field.");
    }
}

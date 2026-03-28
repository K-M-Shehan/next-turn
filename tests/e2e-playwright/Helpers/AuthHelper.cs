using System.Collections.Concurrent;
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

    public static async Task<string> GetBearerTokenAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{username}:{password}";
        if (TokenCache.TryGetValue(cacheKey, out var cachedToken))
        {
            return cachedToken;
        }

        var requestBody = new
        {
            username,
            password,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(BaseUrl), "/api/auth/login"))
        {
            Content = JsonContent.Create(requestBody),
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AssertionException(
                $"Authentication failed with status code {(int)response.StatusCode}. Body: {content}");
        }

        var token = ExtractToken(content);
        TokenCache[cacheKey] = token;
        return token;
    }

    public static async Task ApplyAuthToContextAsync(IBrowserContext context, string username, string password, CancellationToken cancellationToken = default)
    {
        var token = await GetBearerTokenAsync(username, password, cancellationToken);

        await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {token}",
        });

        var serializedToken = JsonSerializer.Serialize(token);
        await context.AddInitScriptAsync(
            $"window.localStorage.setItem('token', {serializedToken});" +
            $"window.localStorage.setItem('accessToken', {serializedToken});" +
            $"window.localStorage.setItem('authToken', {serializedToken});");
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

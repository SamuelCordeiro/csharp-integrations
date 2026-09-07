using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers the bearer login endpoint and its HTTP security boundary.
/// </summary>
public sealed class AuthBearerEndpointsTests
{
    /// <summary>
    /// Verifies that demonstration credentials produce an access token.
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "123" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();

        Assert.NotNull(result);
        Assert.Equal("Josh", result.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    }

    /// <summary>
    /// Verifies that invalid credentials are not disclosed as a successful login.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "invalid" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Verifies that an endpoint marked with <c>Authorize</c> rejects anonymous calls.
    /// </summary>
    [Fact]
    public async Task ProtectedEndpoint_WithoutAccessToken_ReturnsUnauthorized()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/Ollama/chat",
            new { prompt = "Hello" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the login endpoint eventually rejects excess requests with HTTP 429.
    /// </summary>
    [Fact]
    public async Task Login_WhenRateLimitIsExceeded_ReturnsTooManyRequests()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt < 6 && response?.StatusCode != HttpStatusCode.TooManyRequests; attempt++)
        {
            response = await client.PostAsJsonAsync(
                "/Auth/Bearer/AuthBearer/Login",
                new { username = "Josh", password = "invalid" });
        }

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Verifies that an unhandled upstream integration failure uses a safe ProblemDetails response.
    /// </summary>
    [Fact]
    public async Task Chat_WhenOllamaIsUnavailable_ReturnsServiceUnavailableProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var loginResponse = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "123" });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Ollama/chat")
        {
            Content = JsonContent.Create(new { prompt = "Hello" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var response = await client.SendAsync(request);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemResult>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(problemDetails);
        Assert.Equal(503, problemDetails.Status);
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.TraceId));
        Assert.Null(problemDetails.Detail);
    }

    private sealed class LoginResult
    {
        public required string Username { get; init; }

        public required string AccessToken { get; init; }
    }

    private sealed class ProblemResult
    {
        public int Status { get; init; }

        public string? Detail { get; init; }

        public string? TraceId { get; init; }
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
}

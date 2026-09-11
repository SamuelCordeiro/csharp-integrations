using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers administrative user access endpoints.
/// </summary>
public sealed class AdminUserAccessControllerTests
{
    /// <summary>
    /// Verifies that a temporary lock blocks login until an administrator unlocks the user.
    /// </summary>
    [Fact]
    public async Task LockAndUnlockUser_ChangesAuthenticationAvailability()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var user = await GetUserAsync(factory, "Josh");
        var lockRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/lock",
            accessToken,
            new { durationMinutes = 30 });

        var lockResponse = await client.SendAsync(lockRequest);
        var lockedUser = await GetUserAsync(factory, "Josh");
        var loginWhileLocked = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "Demo#123" });
        var unlockResponse = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/unlock",
            accessToken));
        var unlockedUser = await GetUserAsync(factory, "Josh");
        var loginAfterUnlock = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "Demo#123" });

        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);
        Assert.True(lockedUser.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.Unauthorized, loginWhileLocked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unlockResponse.StatusCode);
        Assert.Null(unlockedUser.LockoutEnd);
        Assert.Equal(0, unlockedUser.AccessFailedCount);
        Assert.Equal(HttpStatusCode.OK, loginAfterUnlock.StatusCode);
    }

    /// <summary>
    /// Verifies that disabling a user blocks access tokens, login, and refresh tokens.
    /// </summary>
    [Fact]
    public async Task DisableUser_RevokesAuthenticationAvailability()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory, handleCookies: false);
        var administratorToken = await LoginAsync(client, "Admin");
        var loginResponse = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Alice", password = "Demo#123" });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        var refreshToken = GetRefreshTokenValue(loginResponse);
        var user = await GetUserAsync(factory, "Alice");

        var disableResponse = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/disable",
            administratorToken));
        var disabledUser = await GetUserAsync(factory, "Alice");
        var protectedResponse = await client.SendAsync(CreateBearerRequest(
            HttpMethod.Post,
            "/api/Ollama/chat",
            login!.AccessToken,
            new { prompt = "Hello" }));
        var refreshResponse = await client.SendAsync(CreateRefreshRequest(refreshToken));
        var loginAfterDisable = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Alice", password = "Demo#123" });

        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);
        Assert.False(disabledUser.IsActive);
        Assert.NotNull(disabledUser.DisabledAtUtc);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfterDisable.StatusCode);
    }

    /// <summary>
    /// Verifies that enabling a disabled user restores password authentication.
    /// </summary>
    [Fact]
    public async Task EnableUser_AfterDisable_RestoresAuthenticationAvailability()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var administratorToken = await LoginAsync(client, "Admin");
        var user = await GetUserAsync(factory, "Alice");
        await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/disable",
            administratorToken));

        var enableResponse = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/enable",
            administratorToken));
        var enabledUser = await GetUserAsync(factory, "Alice");
        var loginResponse = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Alice", password = "Demo#123" });

        Assert.Equal(HttpStatusCode.NoContent, enableResponse.StatusCode);
        Assert.True(enabledUser.IsActive);
        Assert.Null(enabledUser.DisabledAtUtc);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that the last active administrator cannot be locked.
    /// </summary>
    [Fact]
    public async Task LockUser_WhenTargetIsLastActiveAdministrator_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var administratorToken = await LoginAsync(client, "Admin");
        var administrator = await GetUserAsync(factory, "Admin");

        var response = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{administrator.Id}/lock",
            administratorToken,
            new { durationMinutes = 30 }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Verifies that the last active administrator cannot be disabled.
    /// </summary>
    [Fact]
    public async Task DisableUser_WhenTargetIsLastActiveAdministrator_ReturnsConflictProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var administratorToken = await LoginAsync(client, "Admin");
        var administrator = await GetUserAsync(factory, "Admin");

        var response = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{administrator.Id}/disable",
            administratorToken));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory, bool handleCookies = true) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies
        });

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? content = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        return request;
    }

    private static HttpRequestMessage CreateBearerRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object content)
    {
        var request = CreateAuthorizedRequest(method, requestUri, accessToken, content);
        return request;
    }

    private static async Task<string> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username, password = "Demo#123" });
        var login = await response.Content.ReadFromJsonAsync<LoginResult>();

        response.EnsureSuccessStatusCode();
        return login!.AccessToken;
    }

    private static async Task<ApplicationUser> GetUserAsync(ApiFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.FindByNameAsync(username))!;
    }

    private static HttpRequestMessage CreateRefreshRequest(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/Auth/Bearer/AuthBearer/Refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        return request;
    }

    private static string GetRefreshTokenValue(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith("refresh_token=", StringComparison.Ordinal));
        const string cookieName = "refresh_token=";
        var valueStart = cookie.IndexOf(cookieName, StringComparison.Ordinal) + cookieName.Length;
        var valueEnd = cookie.IndexOf(';', valueStart);

        return cookie[valueStart..valueEnd];
    }

    private sealed class LoginResult
    {
        public required string AccessToken { get; init; }
    }
}

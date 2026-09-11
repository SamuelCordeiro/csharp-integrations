using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers password reset endpoints.
/// </summary>
public sealed class PasswordResetEndpointsTests
{
    /// <summary>
    /// Verifies that an administrator can initiate a reset without exposing its token.
    /// </summary>
    [Fact]
    public async Task RequestPasswordReset_DeliversTokenInternallyAndRequiresPasswordChange()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var administratorToken = await LoginAsync(client, "Admin", "Demo#123");
        var userToken = await LoginAsync(client, "Josh", "Demo#123");
        var user = await GetUserAsync(factory, "Josh");
        var notifier = factory.Services.GetRequiredService<IPasswordResetNotifier>() as TestPasswordResetNotifier;

        var response = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/password-reset",
            administratorToken));
        var updatedUser = await GetUserAsync(factory, "Josh");
        var protectedResponse = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/Ollama/chat",
            userToken,
            new { prompt = "Hello" }));
        var loginResponse = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "Demo#123" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.NotNull(notifier?.LastNotification);
        Assert.Equal(user.Id, notifier.LastNotification.UserId);
        Assert.Equal("josh@example.test", notifier.LastNotification.Email);
        Assert.True(updatedUser.MustChangePassword);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that a delivered token resets the password and invalidates prior access tokens.
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithDeliveredToken_ChangesPasswordAndRestoresAccess()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var administratorToken = await LoginAsync(client, "Admin", "Demo#123");
        var previousUserToken = await LoginAsync(client, "Josh", "Demo#123");
        var user = await GetUserAsync(factory, "Josh");
        var notifier = factory.Services.GetRequiredService<IPasswordResetNotifier>() as TestPasswordResetNotifier;
        await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/password-reset",
            administratorToken));

        var resetResponse = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/ResetPassword",
            new
            {
                userId = user.Id,
                token = notifier!.LastNotification!.Token,
                newPassword = "Changed#123"
            });
        var updatedUser = await GetUserAsync(factory, "Josh");
        var previousTokenResponse = await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/Ollama/chat",
            previousUserToken,
            new { prompt = "Hello" }));
        var loginResponse = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password = "Changed#123" });

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);
        Assert.True(updatedUser.IsActive);
        Assert.False(updatedUser.MustChangePassword);
        Assert.Equal(HttpStatusCode.Unauthorized, previousTokenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that passwords violating the active policy cannot complete a reset.
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithPasswordViolatingPolicy_ReturnsValidationProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var administratorToken = await LoginAsync(client, "Admin", "Demo#123");
        var user = await GetUserAsync(factory, "Josh");
        var notifier = factory.Services.GetRequiredService<IPasswordResetNotifier>() as TestPasswordResetNotifier;
        await client.SendAsync(CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/admin/users/{user.Id}/password-reset",
            administratorToken));

        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/ResetPassword",
            new { userId = user.Id, token = notifier!.LastNotification!.Token, newPassword = "weak" });
        var updatedUser = await GetUserAsync(factory, "Josh");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(updatedUser.MustChangePassword);
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
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

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username, password });
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

    private sealed class LoginResult
    {
        public required string AccessToken { get; init; }
    }
}

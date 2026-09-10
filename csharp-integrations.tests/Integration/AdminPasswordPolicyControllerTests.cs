using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers administrative password policy endpoints.
/// </summary>
public sealed class AdminPasswordPolicyControllerTests
{
    /// <summary>
    /// Verifies that an administrator can read the active password policy.
    /// </summary>
    [Fact]
    public async Task Get_WithAdministratorAccessToken_ReturnsPasswordPolicy()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/password-policy");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var policy = await response.Content.ReadFromJsonAsync<PasswordPolicyResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(policy);
        Assert.Equal(8, policy.MinimumLength);
        Assert.True(policy.RequireDigit);
    }

    /// <summary>
    /// Verifies that non-administrators cannot read the active password policy.
    /// </summary>
    [Fact]
    public async Task Get_WithEmployeeAccessToken_ReturnsForbidden()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Alice");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/password-policy");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Verifies that an administrator can update and audit the active password policy.
    /// </summary>
    [Fact]
    public async Task Update_WithAdministratorAccessToken_PersistsPolicyAndActor()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/password-policy")
        {
            Content = JsonContent.Create(new
            {
                minimumLength = 12,
                requireUppercase = true,
                requireLowercase = true,
                requireDigit = true,
                requireNonAlphanumeric = true,
                requiredUniqueCharacters = 4,
                maxFailedAccessAttempts = 3,
                lockoutDurationMinutes = 30
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<PasswordPolicyResult>();
        var policy = await GetPolicyAsync(factory);
        var administrator = await GetUserAsync(factory, "Admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(12, result.MinimumLength);
        Assert.Equal(3, result.MaxFailedAccessAttempts);
        Assert.Equal(30, result.LockoutDurationMinutes);
        Assert.Equal(administrator.Id, result.UpdatedByUserId);
        Assert.Equal(12, policy.MinimumLength);
        Assert.Equal(3, policy.MaxFailedAccessAttempts);
        Assert.Equal(30, policy.LockoutDurationMinutes);
        Assert.Equal(administrator.Id, policy.UpdatedByUserId);
    }

    /// <summary>
    /// Verifies that invalid policy limits return validation ProblemDetails.
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidPolicyLimit_ReturnsValidationProblemDetails()
    {
        using var factory = new ApiFactory();
        var client = CreateHttpsClient(factory);
        var accessToken = await LoginAsync(client, "Admin");
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/password-policy")
        {
            Content = JsonContent.Create(new
            {
                minimumLength = 7,
                requireUppercase = true,
                requireLowercase = true,
                requireDigit = true,
                requireNonAlphanumeric = true,
                requiredUniqueCharacters = 1,
                maxFailedAccessAttempts = 0,
                lockoutDurationMinutes = 0
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task<string> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username, password = "Demo#123" });
        var login = await response.Content.ReadFromJsonAsync<LoginResult>();

        response.EnsureSuccessStatusCode();
        return login!.AccessToken;
    }

    private static async Task<PasswordPolicy> GetPolicyAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await database.PasswordPolicies.AsNoTracking().SingleAsync();
    }

    private static async Task<ApplicationUser> GetUserAsync(ApiFactory factory, string username)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await database.Users.AsNoTracking().SingleAsync(user => user.UserName == username);
    }

    private sealed class LoginResult
    {
        public required string AccessToken { get; init; }
    }

    private sealed class PasswordPolicyResult
    {
        public required int MinimumLength { get; init; }

        public required bool RequireDigit { get; init; }

        public required int MaxFailedAccessAttempts { get; init; }

        public required int LockoutDurationMinutes { get; init; }

        public int? UpdatedByUserId { get; init; }
    }
}

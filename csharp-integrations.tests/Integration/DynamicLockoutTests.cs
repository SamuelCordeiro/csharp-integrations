using System.Net;
using System.Net.Http.Json;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers lockout behavior driven by the active password policy.
/// </summary>
public sealed class DynamicLockoutTests
{
    /// <summary>
    /// Verifies that the configured failed-attempt limit locks the user.
    /// </summary>
    [Fact]
    public async Task Login_AfterConfiguredFailedAttemptLimit_ReturnsUnauthorizedAndLocksUser()
    {
        using var factory = new ApiFactory();
        await UpdatePolicyAsync(factory, maximumFailedAttempts: 2, lockoutDurationMinutes: 15);
        var client = CreateHttpsClient(factory);

        var firstAttempt = await LoginAsync(client, "invalid");
        var secondAttempt = await LoginAsync(client, "invalid");
        var validAttempt = await LoginAsync(client, "Demo#123");
        var user = await GetUserAsync(factory);

        Assert.Equal(HttpStatusCode.Unauthorized, firstAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, validAttempt.StatusCode);
        Assert.Equal(2, user.AccessFailedCount);
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow.AddMinutes(14));
    }

    /// <summary>
    /// Verifies that a successful login clears failed attempts before lockout.
    /// </summary>
    [Fact]
    public async Task Login_AfterSuccessfulAuthentication_ResetsFailedAttempts()
    {
        using var factory = new ApiFactory();
        await UpdatePolicyAsync(factory, maximumFailedAttempts: 3, lockoutDurationMinutes: 15);
        var client = CreateHttpsClient(factory);

        var invalidAttempt = await LoginAsync(client, "invalid");
        var validAttempt = await LoginAsync(client, "Demo#123");
        var user = await GetUserAsync(factory);

        Assert.Equal(HttpStatusCode.Unauthorized, invalidAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.OK, validAttempt.StatusCode);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string password)
    {
        return await client.PostAsJsonAsync(
            "/Auth/Bearer/AuthBearer/Login",
            new { username = "Josh", password });
    }

    private static HttpClient CreateHttpsClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static async Task UpdatePolicyAsync(
        ApiFactory factory,
        int maximumFailedAttempts,
        int lockoutDurationMinutes)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var policy = await database.PasswordPolicies.SingleAsync();
        policy.MaxFailedAccessAttempts = maximumFailedAttempts;
        policy.LockoutDurationMinutes = lockoutDurationMinutes;
        policy.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> GetUserAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await database.Users
            .AsNoTracking()
            .SingleAsync(user => user.UserName == "Josh");
    }
}

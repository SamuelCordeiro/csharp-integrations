using csharp_integrations.api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Covers persistence and initialization of the active password policy.
/// </summary>
public sealed class PasswordPolicyTests
{
    /// <summary>
    /// Verifies that application startup creates one default password policy.
    /// </summary>
    [Fact]
    public async Task GetAsync_AfterApplicationStarts_ReturnsSeededDefaultPolicy()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var passwordPolicyService = scope.ServiceProvider.GetRequiredService<PasswordPolicyService>();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var policy = await passwordPolicyService.GetAsync();
        var policyCount = await database.PasswordPolicies.CountAsync();

        Assert.Equal(1, policyCount);
        Assert.Equal(PasswordPolicy.ActivePolicyId, policy.Id);
        Assert.Equal(8, policy.MinimumLength);
        Assert.True(policy.RequireUppercase);
        Assert.True(policy.RequireLowercase);
        Assert.True(policy.RequireDigit);
        Assert.True(policy.RequireNonAlphanumeric);
        Assert.Equal(1, policy.RequiredUniqueCharacters);
        Assert.Equal(5, policy.MaxFailedAccessAttempts);
        Assert.Equal(5, policy.LockoutDurationMinutes);
        Assert.Null(policy.UpdatedByUserId);
    }
}

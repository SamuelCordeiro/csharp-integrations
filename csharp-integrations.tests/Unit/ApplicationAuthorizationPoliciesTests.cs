using System.Security.Claims;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.tests.Unit;

/// <summary>
/// Covers authorization policies for identity administration.
/// </summary>
public sealed class ApplicationAuthorizationPoliciesTests
{
    /// <summary>
    /// Verifies that administrators can use identity administration policies.
    /// </summary>
    /// <param name="policyName">Policy evaluated by the authorization service.</param>
    [Theory]
    [InlineData(ApplicationAuthorizationPolicies.ManageUsers)]
    [InlineData(ApplicationAuthorizationPolicies.ManagePasswordPolicy)]
    public async Task AuthorizeAsync_WithAdministratorRole_Succeeds(string policyName)
    {
        using var provider = CreateServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var administrator = CreatePrincipal(ApplicationRoles.Administrator);

        var result = await authorizationService.AuthorizeAsync(administrator, resource: null, policyName);

        Assert.True(result.Succeeded);
    }

    /// <summary>
    /// Verifies that non-administrators cannot use identity administration policies.
    /// </summary>
    /// <param name="role">Role assigned to the authenticated user.</param>
    /// <param name="policyName">Policy evaluated by the authorization service.</param>
    [Theory]
    [InlineData(ApplicationRoles.Employee, ApplicationAuthorizationPolicies.ManageUsers)]
    [InlineData(ApplicationRoles.Manager, ApplicationAuthorizationPolicies.ManagePasswordPolicy)]
    public async Task AuthorizeAsync_WithNonAdministratorRole_Fails(string role, string policyName)
    {
        using var provider = CreateServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var user = CreatePrincipal(role);

        var result = await authorizationService.AuthorizeAsync(user, resource: null, policyName);

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(ApplicationAuthorizationPolicies.Configure);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreatePrincipal(string role) => new(
        new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], authenticationType: "Test"));
}

using Microsoft.AspNetCore.Authorization;

namespace csharp_integrations.api.Data;

/// <summary>
/// Defines authorization policies used by the application.
/// </summary>
public static class ApplicationAuthorizationPolicies
{
    /// <summary>
    /// Identifies the policy required to manage users.
    /// </summary>
    public const string ManageUsers = "ManageUsers";

    /// <summary>
    /// Identifies the policy required to manage password policies.
    /// </summary>
    public const string ManagePasswordPolicy = "ManagePasswordPolicy";

    /// <summary>
    /// Identifies the policy required to manage Ollama models.
    /// </summary>
    public const string CanManageModels = "CanManageModels";

    /// <summary>
    /// Configures application authorization policies.
    /// </summary>
    /// <param name="options">Authorization options to configure.</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManageUsers, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator));
        options.AddPolicy(ManagePasswordPolicy, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator));
        options.AddPolicy(CanManageModels, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Manager));
    }
}

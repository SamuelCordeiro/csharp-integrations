namespace csharp_integrations.api.Data;

/// <summary>
/// Defines roles used by application authorization policies.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>
    /// Gets the roles supported by the application.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [Employee, Manager, Administrator];

    /// <summary>
    /// Grants access to standard authenticated features.
    /// </summary>
    public const string Employee = "employee";

    /// <summary>
    /// Grants access to model management operations.
    /// </summary>
    public const string Manager = "manager";

    /// <summary>
    /// Grants access to identity administration operations.
    /// </summary>
    public const string Administrator = "administrator";
}

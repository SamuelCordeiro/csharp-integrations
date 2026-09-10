namespace csharp_integrations.api.Data;

/// <summary>
/// Contains changes for the active password policy.
/// </summary>
public sealed class PasswordPolicyUpdate
{
    /// <summary>
    /// Gets or sets the minimum password length.
    /// </summary>
    public int MinimumLength { get; init; }

    /// <summary>
    /// Gets or sets whether uppercase characters are required.
    /// </summary>
    public bool RequireUppercase { get; init; }

    /// <summary>
    /// Gets or sets whether lowercase characters are required.
    /// </summary>
    public bool RequireLowercase { get; init; }

    /// <summary>
    /// Gets or sets whether numeric characters are required.
    /// </summary>
    public bool RequireDigit { get; init; }

    /// <summary>
    /// Gets or sets whether non-alphanumeric characters are required.
    /// </summary>
    public bool RequireNonAlphanumeric { get; init; }

    /// <summary>
    /// Gets or sets the minimum number of unique password characters.
    /// </summary>
    public int RequiredUniqueCharacters { get; init; }

    /// <summary>
    /// Gets or sets the maximum failed sign-in attempts before lockout.
    /// </summary>
    public int MaxFailedAccessAttempts { get; init; }

    /// <summary>
    /// Gets or sets the lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; init; }
}

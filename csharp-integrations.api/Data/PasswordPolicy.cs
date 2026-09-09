namespace csharp_integrations.api.Data;

/// <summary>
/// Defines the active password and lockout requirements.
/// </summary>
public sealed class PasswordPolicy
{
    /// <summary>
    /// Identifies the single active password policy.
    /// </summary>
    public const int ActivePolicyId = 1;

    /// <summary>
    /// Gets or sets the policy identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the minimum password length.
    /// </summary>
    public int MinimumLength { get; set; }

    /// <summary>
    /// Gets or sets whether uppercase characters are required.
    /// </summary>
    public bool RequireUppercase { get; set; }

    /// <summary>
    /// Gets or sets whether lowercase characters are required.
    /// </summary>
    public bool RequireLowercase { get; set; }

    /// <summary>
    /// Gets or sets whether numeric characters are required.
    /// </summary>
    public bool RequireDigit { get; set; }

    /// <summary>
    /// Gets or sets whether non-alphanumeric characters are required.
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of unique password characters.
    /// </summary>
    public int RequiredUniqueCharacters { get; set; }

    /// <summary>
    /// Gets or sets the maximum failed sign-in attempts before lockout.
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; }

    /// <summary>
    /// Gets or sets the lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the UTC date of the latest policy update.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the policy.
    /// </summary>
    public int? UpdatedByUserId { get; set; }
}

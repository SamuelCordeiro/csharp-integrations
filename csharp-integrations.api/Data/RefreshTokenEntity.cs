namespace csharp_integrations.api.Data;

/// <summary>
/// Represents a hashed refresh token persisted for an application user.
/// </summary>
public sealed class RefreshTokenEntity
{
    /// <summary>
    /// Gets or sets the SHA-256 token hash.
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// Gets or sets the token family identifier.
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration date.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC revocation date.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the replacement token hash.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Gets or sets the associated user.
    /// </summary>
    public ApplicationUser? User { get; set; }
}

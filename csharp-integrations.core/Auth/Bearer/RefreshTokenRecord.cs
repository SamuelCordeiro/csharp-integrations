namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Represents a hashed refresh token stored by the application.
/// </summary>
public sealed class RefreshTokenRecord
{
    /// <summary>
    /// Gets the SHA-256 hash of the refresh token.
    /// </summary>
    public required string TokenHash { get; init; }

    /// <summary>
    /// Gets the identifier shared by tokens created in the same session.
    /// </summary>
    public required Guid FamilyId { get; init; }

    /// <summary>
    /// Gets the authenticated user identifier.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the authenticated username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the UTC expiration date.
    /// </summary>
    public required DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the UTC revocation date.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the hash of the token that replaced this token.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }
}

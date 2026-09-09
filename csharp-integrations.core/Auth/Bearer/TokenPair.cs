namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Contains an issued refresh token and its metadata.
/// </summary>
public sealed class RefreshTokenIssue
{
    /// <summary>
    /// Gets the authenticated user identifier.
    /// </summary>
public required int UserId { get; init; }

    /// <summary>
    /// Gets the authenticated username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the opaque refresh token for secure client storage.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the UTC refresh token expiration date.
    /// </summary>
    public required DateTime RefreshTokenExpiresAtUtc { get; init; }
}

/// <summary>
/// Contains the outcome of a refresh request.
/// </summary>
public sealed class RefreshTokenRefreshResult
{
    /// <summary>
    /// Gets the refresh token rotation status.
    /// </summary>
    public required RefreshTokenRotationStatus Status { get; init; }

    /// <summary>
    /// Gets the replacement refresh token when rotation succeeds.
    /// </summary>
    public RefreshTokenIssue? RefreshTokenIssue { get; init; }
}

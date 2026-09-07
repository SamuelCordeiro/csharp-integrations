namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Contains the raw tokens issued after authentication or refresh.
/// </summary>
public sealed class TokenPair
{
    /// <summary>
    /// Gets the authenticated username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the short-lived JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the opaque refresh token for secure client storage.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the access token lifetime in seconds.
    /// </summary>
    public required int ExpiresInSeconds { get; init; }

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
    /// Gets the replacement token pair when rotation succeeds.
    /// </summary>
    public TokenPair? TokenPair { get; init; }
}

namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Defines the outcome of a refresh token rotation attempt.
/// </summary>
public enum RefreshTokenRotationStatus
{
    /// <summary>
    /// The token was rotated successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The token was not found or has expired.
    /// </summary>
    Invalid,

    /// <summary>
    /// A previously rotated token was reused and its family was revoked.
    /// </summary>
    Reused
}

/// <summary>
/// Contains the result of a refresh token rotation attempt.
/// </summary>
public sealed class RefreshTokenRotationResult
{
    /// <summary>
    /// Gets the rotation status.
    /// </summary>
    public required RefreshTokenRotationStatus Status { get; init; }

    /// <summary>
    /// Gets the replacement token record when rotation succeeds.
    /// </summary>
    public RefreshTokenRecord? RefreshToken { get; init; }
}

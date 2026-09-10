namespace csharp_integrations.api.Data;

/// <summary>
/// Represents the outcome of password authentication.
/// </summary>
public sealed class PasswordAuthenticationResult
{
    /// <summary>
    /// Gets the authentication status.
    /// </summary>
    public required PasswordAuthenticationStatus Status { get; init; }

    /// <summary>
    /// Gets the authenticated user when authentication succeeds.
    /// </summary>
    public ApplicationUser? User { get; init; }
}

/// <summary>
/// Defines password authentication outcomes.
/// </summary>
public enum PasswordAuthenticationStatus
{
    /// <summary>
    /// Authentication completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Authentication failed without exposing account details.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// Authentication was denied because the account is locked.
    /// </summary>
    LockedOut
}

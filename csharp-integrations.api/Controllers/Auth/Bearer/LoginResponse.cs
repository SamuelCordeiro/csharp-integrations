namespace csharp_integrations.api.Controllers.Auth.Bearer;

/// <summary>
/// Represents the successful result of a bearer-token login.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// Gets the authenticated username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the short-lived JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace csharp_integrations.api.Controllers.Auth.Bearer;

/// <summary>
/// Represents credentials submitted to the bearer login endpoint.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [Required]
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    public required string Password { get; init; }
}

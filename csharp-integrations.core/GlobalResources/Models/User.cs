using System.ComponentModel.DataAnnotations;

namespace csharp_integrations.core.GlobalResources.Models;

/// <summary>
/// Represents a user used by the authentication demonstration.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [Required]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the password. This property is only used by the in-memory demonstration repository.
    /// </summary>
    [Required]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the user role.
    /// </summary>
    public required string Role { get; set; }
}

/// <summary>
/// Represents credentials submitted to the bearer login endpoint.
/// </summary>
public class UserLogin
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [Required]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    public required string Password { get; set; }
}

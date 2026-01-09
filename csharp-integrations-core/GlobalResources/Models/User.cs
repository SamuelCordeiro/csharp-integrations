namespace csharp_integrations_core.GlobalResources.Models;

/// <summary>
/// User data structure model
/// </summary>
public class User
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

/// <summary>
/// User login data structure model
/// </summary>
public class UserLogin
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
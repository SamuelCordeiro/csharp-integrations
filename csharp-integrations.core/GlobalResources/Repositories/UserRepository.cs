using csharp_integrations.core.GlobalResources.Models;

namespace csharp_integrations.core.GlobalResources.Repositories;

/// <summary>
/// Provides the in-memory users used exclusively by the authentication demonstration.
/// </summary>
public static class UserRepository
{
    /// <summary>
    /// Finds a demonstration user with the supplied credentials.
    /// </summary>
    /// <param name="username">Username to match.</param>
    /// <param name="password">Password to match.</param>
    /// <returns>The matching user, or <see langword="null"/> when the credentials are invalid.</returns>
    public static User? Get(string username, string password)
    {
        return Users.FirstOrDefault(x => x.Username == username && x.Password == password);
    }

    /// <summary>
    /// Finds a demonstration user by identifier.
    /// </summary>
    /// <param name="userId">User identifier to match.</param>
    /// <returns>The matching user, or <see langword="null"/> when no user is found.</returns>
    public static User? GetById(int userId)
    {
        return Users.FirstOrDefault(user => user.Id == userId);
    }

    private static readonly IReadOnlyList<User> Users =
    [
        new User { Id = 1, Username = "Josh", Password = "123", Role = "manager" },
        new User { Id = 2, Username = "Alice", Password = "123", Role = "employee" }
    ];
}

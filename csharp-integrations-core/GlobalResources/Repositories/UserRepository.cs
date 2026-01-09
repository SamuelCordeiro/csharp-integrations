using csharp_integrations_core.GlobalResources.Models;

namespace csharp_integrations_core.GlobalResources.Repositories;

public static class UserRepository
{
    /// <summary>
    /// User data search simulation
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns></returns>
    public static User Get(string username, string password)
    {
        var users = new List<User>
        {
            new() { Id = 1, Username = "Josh", Password = "123", Role = "manager" },
            new() { Id = 2, Username = "Alice", Password = "123", Role = "employee" }
        };

        return users.First(x => x.Username == username && x.Password == password);
    }
}
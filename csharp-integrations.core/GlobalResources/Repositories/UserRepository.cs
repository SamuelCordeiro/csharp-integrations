using csharp_integrations.core.GlobalResources.Models;

namespace csharp_integrations.core.GlobalResources.Repositories;

public static class UserRepository
{
    public static User Get(string username, string password)
    {
        var users = new List<User>
        {
            new User { Id = 1, Username = "Josh", Password = "123", Role = "manager" },
            new User { Id = 2, Username = "Alice", Password = "123", Role = "employee" }
        };

        return users.Where(x => x.Username == username && x.Password == password).First();
    }
}
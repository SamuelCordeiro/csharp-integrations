using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Seeds demonstration roles and users for local development.
/// </summary>
public sealed class IdentityDataSeeder(
    RoleManager<IdentityRole<int>> roleManager,
    UserManager<ApplicationUser> userManager)
{
    /// <summary>
    /// Creates the demonstration roles and users when they do not exist.
    /// </summary>
    public async Task SeedAsync()
    {
        await EnsureRoleAsync(ApplicationRoles.Employee);
        await EnsureRoleAsync(ApplicationRoles.Manager);
        await EnsureRoleAsync(ApplicationRoles.Administrator);
        await EnsureUserAsync("Josh", "Demo#123", ApplicationRoles.Manager);
        await EnsureUserAsync("Alice", "Demo#123", ApplicationRoles.Employee);
        await EnsureUserAsync("Admin", "Demo#123", ApplicationRoles.Administrator);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        }
    }

    private async Task EnsureUserAsync(string username, string password, string roleName)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            user = new ApplicationUser { UserName = username };
            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to seed user '{username}'.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}

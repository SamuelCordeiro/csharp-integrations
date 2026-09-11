using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Manages users and their application roles.
/// </summary>
public sealed class UserManagementService(UserManager<ApplicationUser> userManager)
{
    /// <summary>
    /// Creates a user with the requested roles.
    /// </summary>
    /// <param name="username">Username for the new user.</param>
    /// <param name="password">Password for the new user.</param>
    /// <param name="requestedRoles">Roles assigned to the new user.</param>
    /// <returns>The creation result.</returns>
    public async Task<UserManagementResult> CreateAsync(
        string username,
        string password,
        IReadOnlyCollection<string> requestedRoles)
    {
        var roles = NormalizeRoles(requestedRoles);
        if (roles is null)
        {
            return UserManagementResult.InvalidRoles();
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            LockoutEnabled = true
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return UserManagementResult.Failed(createResult.Errors);
        }

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return UserManagementResult.Failed(roleResult.Errors);
        }

        return UserManagementResult.Succeeded(user.Id);
    }

    /// <summary>
    /// Replaces the roles assigned to a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="requestedRoles">Roles that must remain assigned.</param>
    /// <returns>The role update result.</returns>
    public async Task<UserManagementResult> UpdateRolesAsync(
        int userId,
        IReadOnlyCollection<string> requestedRoles)
    {
        var roles = NormalizeRoles(requestedRoles);
        if (roles is null)
        {
            return UserManagementResult.InvalidRoles();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserManagementResult.NotFound();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var removesAdministrator = currentRoles.Contains(ApplicationRoles.Administrator, StringComparer.OrdinalIgnoreCase)
            && !roles.Contains(ApplicationRoles.Administrator, StringComparer.OrdinalIgnoreCase);

        if (removesAdministrator && await IsLastActiveAdministratorAsync(user))
        {
            return UserManagementResult.LastActiveAdministrator();
        }

        var rolesToAdd = roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
        if (!addResult.Succeeded)
        {
            return UserManagementResult.Failed(addResult.Errors);
        }

        var rolesToRemove = currentRoles.Except(roles, StringComparer.OrdinalIgnoreCase).ToArray();
        var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
        if (!removeResult.Succeeded)
        {
            return UserManagementResult.Failed(removeResult.Errors);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);

        return updateResult.Succeeded
            ? UserManagementResult.Succeeded(user.Id)
            : UserManagementResult.Failed(updateResult.Errors);
    }

    private async Task<bool> IsLastActiveAdministratorAsync(ApplicationUser user)
    {
        if (!user.IsActive || user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            return false;
        }

        var administrators = await userManager.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        var activeAdministratorCount = administrators.Count(administrator =>
            administrator.IsActive
            && (administrator.LockoutEnd is null || administrator.LockoutEnd <= DateTimeOffset.UtcNow));

        return activeAdministratorCount == 1;
    }

    private static IReadOnlyList<string>? NormalizeRoles(IReadOnlyCollection<string> requestedRoles)
    {
        if (requestedRoles.Count == 0 || requestedRoles.Any(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var roles = requestedRoles
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roles.Any(role => !ApplicationRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            return null;
        }

        return ApplicationRoles.All
            .Where(allowedRole => roles.Contains(allowedRole, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }
}

/// <summary>
/// Represents the outcome of a user management operation.
/// </summary>
public sealed class UserManagementResult
{
    /// <summary>
    /// Gets the operation status.
    /// </summary>
    public required UserManagementStatus Status { get; init; }

    /// <summary>
    /// Gets the affected user identifier.
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// Gets validation errors returned by Identity.
    /// </summary>
    public IReadOnlyList<IdentityError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="userId">Affected user identifier.</param>
    /// <returns>A successful result.</returns>
    public static UserManagementResult Succeeded(int userId) => new()
    {
        Status = UserManagementStatus.Succeeded,
        UserId = userId
    };

    /// <summary>
    /// Creates a user-not-found result.
    /// </summary>
    /// <returns>A user-not-found result.</returns>
    public static UserManagementResult NotFound() => new()
    {
        Status = UserManagementStatus.NotFound
    };

    /// <summary>
    /// Creates an invalid-role result.
    /// </summary>
    /// <returns>An invalid-role result.</returns>
    public static UserManagementResult InvalidRoles() => new()
    {
        Status = UserManagementStatus.InvalidRoles
    };

    /// <summary>
    /// Creates a last-active-administrator result.
    /// </summary>
    /// <returns>A last-active-administrator result.</returns>
    public static UserManagementResult LastActiveAdministrator() => new()
    {
        Status = UserManagementStatus.LastActiveAdministrator
    };

    /// <summary>
    /// Creates a failed Identity result.
    /// </summary>
    /// <param name="errors">Identity validation errors.</param>
    /// <returns>A failed result.</returns>
    public static UserManagementResult Failed(IEnumerable<IdentityError> errors) => new()
    {
        Status = UserManagementStatus.ValidationFailed,
        Errors = errors.ToArray()
    };
}

/// <summary>
/// Defines user management operation statuses.
/// </summary>
public enum UserManagementStatus
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The user was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The request contains unsupported roles.
    /// </summary>
    InvalidRoles,

    /// <summary>
    /// The operation would remove the last active administrator.
    /// </summary>
    LastActiveAdministrator,

    /// <summary>
    /// Identity validation failed.
    /// </summary>
    ValidationFailed
}

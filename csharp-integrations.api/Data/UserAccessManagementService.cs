using csharp_integrations.core.Auth.Bearer;
using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Manages user lockout and activation state.
/// </summary>
public sealed class UserAccessManagementService(
    UserManager<ApplicationUser> userManager,
    RefreshTokenService refreshTokenService)
{
    /// <summary>
    /// Locks a user until the requested UTC expiration.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="duration">Lockout duration.</param>
    /// <returns>The lock operation result.</returns>
    public async Task<UserAccessManagementResult> LockAsync(int userId, TimeSpan duration)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserAccessManagementResult.NotFound();
        }

        if (await IsLastActiveAdministratorAsync(user))
        {
            return UserAccessManagementResult.LastActiveAdministrator();
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.Add(duration);
        user.AccessFailedCount = 0;
        return await UpdateAsync(user);
    }

    /// <summary>
    /// Clears a user's lockout state.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The unlock operation result.</returns>
    public async Task<UserAccessManagementResult> UnlockAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserAccessManagementResult.NotFound();
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        return await UpdateAsync(user);
    }

    /// <summary>
    /// Disables a user and revokes every refresh token.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The disable operation result.</returns>
    public async Task<UserAccessManagementResult> DisableAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserAccessManagementResult.NotFound();
        }

        if (await IsLastActiveAdministratorAsync(user))
        {
            return UserAccessManagementResult.LastActiveAdministrator();
        }

        user.IsActive = false;
        user.DisabledAtUtc = DateTime.UtcNow;
        var result = await UpdateAsync(user);

        if (result.Status == UserAccessManagementStatus.Succeeded)
        {
            await refreshTokenService.RevokeUserTokensAsync(user.Id);
        }

        return result;
    }

    /// <summary>
    /// Enables a previously disabled user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The enable operation result.</returns>
    public async Task<UserAccessManagementResult> EnableAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserAccessManagementResult.NotFound();
        }

        user.IsActive = true;
        user.DisabledAtUtc = null;
        return await UpdateAsync(user);
    }

    private async Task<bool> IsLastActiveAdministratorAsync(ApplicationUser user)
    {
        if (!user.IsActive || user.LockoutEnd > DateTimeOffset.UtcNow
            || !await userManager.IsInRoleAsync(user, ApplicationRoles.Administrator))
        {
            return false;
        }

        var administrators = await userManager.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        var activeAdministratorCount = administrators.Count(administrator =>
            administrator.IsActive
            && (administrator.LockoutEnd is null || administrator.LockoutEnd <= DateTimeOffset.UtcNow));

        return activeAdministratorCount == 1;
    }

    private async Task<UserAccessManagementResult> UpdateAsync(ApplicationUser user)
    {
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded
            ? UserAccessManagementResult.Succeeded()
            : UserAccessManagementResult.Failed(result.Errors);
    }
}

/// <summary>
/// Represents the outcome of a user access operation.
/// </summary>
public sealed class UserAccessManagementResult
{
    /// <summary>
    /// Gets the operation status.
    /// </summary>
    public required UserAccessManagementStatus Status { get; init; }

    /// <summary>
    /// Gets errors returned by Identity.
    /// </summary>
    public IReadOnlyList<IdentityError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static UserAccessManagementResult Succeeded() => new()
    {
        Status = UserAccessManagementStatus.Succeeded
    };

    /// <summary>
    /// Creates a user-not-found result.
    /// </summary>
    /// <returns>A user-not-found result.</returns>
    public static UserAccessManagementResult NotFound() => new()
    {
        Status = UserAccessManagementStatus.NotFound
    };

    /// <summary>
    /// Creates a last-active-administrator result.
    /// </summary>
    /// <returns>A last-active-administrator result.</returns>
    public static UserAccessManagementResult LastActiveAdministrator() => new()
    {
        Status = UserAccessManagementStatus.LastActiveAdministrator
    };

    /// <summary>
    /// Creates a failed Identity result.
    /// </summary>
    /// <param name="errors">Identity errors.</param>
    /// <returns>A failed result.</returns>
    public static UserAccessManagementResult Failed(IEnumerable<IdentityError> errors) => new()
    {
        Status = UserAccessManagementStatus.ValidationFailed,
        Errors = errors.ToArray()
    };
}

/// <summary>
/// Defines user access operation statuses.
/// </summary>
public enum UserAccessManagementStatus
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
    /// The operation would remove the last active administrator.
    /// </summary>
    LastActiveAdministrator,

    /// <summary>
    /// Identity validation failed.
    /// </summary>
    ValidationFailed
}

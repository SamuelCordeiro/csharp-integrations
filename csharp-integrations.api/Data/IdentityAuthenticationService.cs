using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Authenticates users using the active persisted lockout policy.
/// </summary>
public sealed class IdentityAuthenticationService(
    UserManager<ApplicationUser> userManager,
    PasswordPolicyService passwordPolicyService)
{
    /// <summary>
    /// Authenticates a user and applies the active lockout policy.
    /// </summary>
    /// <param name="username">Username supplied by the client.</param>
    /// <param name="password">Password supplied by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication result.</returns>
    public async Task<PasswordAuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            return CreateResult(PasswordAuthenticationStatus.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return CreateResult(PasswordAuthenticationStatus.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return CreateResult(PasswordAuthenticationStatus.LockedOut);
        }

        if (user.MustChangePassword)
        {
            return CreateResult(PasswordAuthenticationStatus.PasswordChangeRequired);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            var policy = await passwordPolicyService.GetAsync(cancellationToken);
            await RegisterFailedAttemptAsync(user, policy);
            return CreateResult(PasswordAuthenticationStatus.InvalidCredentials);
        }

        await ResetFailedAttemptsAsync(user);
        return new PasswordAuthenticationResult
        {
            Status = PasswordAuthenticationStatus.Succeeded,
            User = user
        };
    }

    private async Task RegisterFailedAttemptAsync(ApplicationUser user, PasswordPolicy policy)
    {
        user.AccessFailedCount++;

        if (user.LockoutEnabled && user.AccessFailedCount >= policy.MaxFailedAccessAttempts)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(policy.LockoutDurationMinutes);
        }

        await UpdateUserAsync(user);
    }

    private async Task ResetFailedAttemptsAsync(ApplicationUser user)
    {
        if (user.AccessFailedCount == 0 && user.LockoutEnd is null)
        {
            return;
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await UpdateUserAsync(user);
    }

    private async Task UpdateUserAsync(ApplicationUser user)
    {
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Unable to update user authentication state.");
        }
    }

    private static PasswordAuthenticationResult CreateResult(PasswordAuthenticationStatus status) => new()
    {
        Status = status
    };
}

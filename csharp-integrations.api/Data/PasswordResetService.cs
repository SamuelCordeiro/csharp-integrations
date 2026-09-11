using csharp_integrations.core.Auth.Bearer;
using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Coordinates password reset requests and completions.
/// </summary>
public sealed class PasswordResetService(
    UserManager<ApplicationUser> userManager,
    RefreshTokenService refreshTokenService,
    IPasswordResetNotifier? passwordResetNotifier = null)
{
    /// <summary>
    /// Creates and delivers a password reset token for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset request result.</returns>
    public async Task<PasswordResetResult> RequestAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return PasswordResetResult.NotFound();
        }

        if (passwordResetNotifier is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return PasswordResetResult.DeliveryUnavailable();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        user.MustChangePassword = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return PasswordResetResult.Failed(updateResult.Errors);
        }

        try
        {
            await passwordResetNotifier.SendAsync(new PasswordResetNotification
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token
            }, cancellationToken);
        }
        catch
        {
            user.MustChangePassword = false;
            await userManager.UpdateAsync(user);
            return PasswordResetResult.DeliveryUnavailable();
        }

        return PasswordResetResult.Succeeded();
    }

    /// <summary>
    /// Resets a user's password with a valid reset token.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="token">Password reset token.</param>
    /// <param name="newPassword">New password validated by the active policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset completion result.</returns>
    public async Task<PasswordResetResult> ResetAsync(
        int userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return PasswordResetResult.NotFound();
        }

        var resetResult = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!resetResult.Succeeded)
        {
            return PasswordResetResult.Failed(resetResult.Errors);
        }

        user.MustChangePassword = false;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return PasswordResetResult.Failed(updateResult.Errors);
        }

        await refreshTokenService.RevokeUserTokensAsync(user.Id, cancellationToken);
        return PasswordResetResult.Succeeded();
    }
}

/// <summary>
/// Delivers password reset notifications through an external channel.
/// </summary>
public interface IPasswordResetNotifier
{
    /// <summary>
    /// Delivers a password reset notification.
    /// </summary>
    /// <param name="notification">Reset notification with a sensitive token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(PasswordResetNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a password reset notification for delivery.
/// </summary>
public sealed class PasswordResetNotification
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public required int UserId { get; init; }

    /// <summary>
    /// Gets the destination email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the sensitive password reset token.
    /// </summary>
    public required string Token { get; init; }
}

/// <summary>
/// Represents the outcome of a password reset operation.
/// </summary>
public sealed class PasswordResetResult
{
    /// <summary>
    /// Gets the operation status.
    /// </summary>
    public required PasswordResetStatus Status { get; init; }

    /// <summary>
    /// Gets errors returned by Identity.
    /// </summary>
    public IReadOnlyList<IdentityError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static PasswordResetResult Succeeded() => new() { Status = PasswordResetStatus.Succeeded };

    /// <summary>
    /// Creates a user-not-found result.
    /// </summary>
    /// <returns>A user-not-found result.</returns>
    public static PasswordResetResult NotFound() => new() { Status = PasswordResetStatus.NotFound };

    /// <summary>
    /// Creates a delivery-unavailable result.
    /// </summary>
    /// <returns>A delivery-unavailable result.</returns>
    public static PasswordResetResult DeliveryUnavailable() => new() { Status = PasswordResetStatus.DeliveryUnavailable };

    /// <summary>
    /// Creates a failed Identity result.
    /// </summary>
    /// <param name="errors">Identity errors.</param>
    /// <returns>A failed result.</returns>
    public static PasswordResetResult Failed(IEnumerable<IdentityError> errors) => new()
    {
        Status = PasswordResetStatus.ValidationFailed,
        Errors = errors.ToArray()
    };
}

/// <summary>
/// Defines password reset operation statuses.
/// </summary>
public enum PasswordResetStatus
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
    /// No password reset delivery channel is available.
    /// </summary>
    DeliveryUnavailable,

    /// <summary>
    /// Identity validation failed.
    /// </summary>
    ValidationFailed
}

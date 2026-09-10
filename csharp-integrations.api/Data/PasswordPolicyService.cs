using Microsoft.EntityFrameworkCore;

namespace csharp_integrations.api.Data;

/// <summary>
/// Retrieves the active password policy.
/// </summary>
public sealed class PasswordPolicyService(ApplicationDbContext database)
{
    /// <summary>
    /// Gets the active password policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active password policy.</returns>
    public Task<PasswordPolicy> GetAsync(CancellationToken cancellationToken = default)
    {
        return database.PasswordPolicies
            .AsNoTracking()
            .SingleAsync(policy => policy.Id == PasswordPolicy.ActivePolicyId, cancellationToken);
    }

    /// <summary>
    /// Updates the active password policy.
    /// </summary>
    /// <param name="update">Validated password policy changes.</param>
    /// <param name="updatedByUserId">Identifier of the administrator applying the changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated password policy.</returns>
    public async Task<PasswordPolicy> UpdateAsync(
        PasswordPolicyUpdate update,
        int updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUpdate(update);

        var policy = await database.PasswordPolicies
            .SingleAsync(policy => policy.Id == PasswordPolicy.ActivePolicyId, cancellationToken);
        policy.MinimumLength = update.MinimumLength;
        policy.RequireUppercase = update.RequireUppercase;
        policy.RequireLowercase = update.RequireLowercase;
        policy.RequireDigit = update.RequireDigit;
        policy.RequireNonAlphanumeric = update.RequireNonAlphanumeric;
        policy.RequiredUniqueCharacters = update.RequiredUniqueCharacters;
        policy.MaxFailedAccessAttempts = update.MaxFailedAccessAttempts;
        policy.LockoutDurationMinutes = update.LockoutDurationMinutes;
        policy.UpdatedAtUtc = DateTime.UtcNow;
        policy.UpdatedByUserId = updatedByUserId;
        await database.SaveChangesAsync(cancellationToken);

        return policy;
    }

    private static void ValidateUpdate(PasswordPolicyUpdate update)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(update.MinimumLength, 8);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(update.MinimumLength, 128);
        ArgumentOutOfRangeException.ThrowIfLessThan(update.RequiredUniqueCharacters, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(update.RequiredUniqueCharacters, 128);
        ArgumentOutOfRangeException.ThrowIfLessThan(update.MaxFailedAccessAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(update.MaxFailedAccessAttempts, 20);
        ArgumentOutOfRangeException.ThrowIfLessThan(update.LockoutDurationMinutes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(update.LockoutDurationMinutes, 1440);
    }
}

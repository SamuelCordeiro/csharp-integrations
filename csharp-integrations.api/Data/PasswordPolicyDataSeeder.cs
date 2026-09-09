using Microsoft.EntityFrameworkCore;

namespace csharp_integrations.api.Data;

/// <summary>
/// Seeds the default active password policy.
/// </summary>
public sealed class PasswordPolicyDataSeeder(ApplicationDbContext database)
{
    /// <summary>
    /// Creates the default policy when no active policy exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await database.PasswordPolicies
            .AnyAsync(policy => policy.Id == PasswordPolicy.ActivePolicyId, cancellationToken);

        if (exists)
        {
            return;
        }

        database.PasswordPolicies.Add(new PasswordPolicy
        {
            Id = PasswordPolicy.ActivePolicyId,
            MinimumLength = 8,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireNonAlphanumeric = true,
            RequiredUniqueCharacters = 1,
            MaxFailedAccessAttempts = 5,
            LockoutDurationMinutes = 5,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await database.SaveChangesAsync(cancellationToken);
    }
}

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
}

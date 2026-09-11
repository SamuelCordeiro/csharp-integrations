using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Represents an application user managed by ASP.NET Identity.
/// </summary>
public sealed class ApplicationUser : IdentityUser<int>
{
    /// <summary>
    /// Gets or sets the UTC date when the user was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC date of the latest user update.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
}

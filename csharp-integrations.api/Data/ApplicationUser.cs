using Microsoft.AspNetCore.Identity;

namespace csharp_integrations.api.Data;

/// <summary>
/// Represents an application user managed by ASP.NET Identity.
/// </summary>
public sealed class ApplicationUser : IdentityUser<int>
{
}

using ITfoxtec.Identity.Saml2.Claims;
using System.Security.Claims;

namespace csharp_integrations_core.Auth.SAML;

public static class ClaimsTransform
{
    /// <summary>
    /// Transforms an authenticated ClaimsPrincipal.
    /// If the user is not authenticated, the original principal is returned.
    /// </summary>
    /// <param name="incomingPrincipal">Claims principal object</param>
    public static ClaimsPrincipal Transform(ClaimsPrincipal incomingPrincipal)
    {
        return incomingPrincipal.Identity is not { IsAuthenticated: true }
            ? incomingPrincipal
            : CreateClaimsPrincipal(incomingPrincipal);
    }
    
    /// <summary>
    /// Creates a new ClaimsPrincipal preserving all incoming claims.
    /// </summary>
    /// <param name="incomingPrincipal">Claims principal object</param>
    private static ClaimsPrincipal CreateClaimsPrincipal(ClaimsPrincipal incomingPrincipal)
    {
        var claims = new List<Claim>();

        // All claims
        claims.AddRange(incomingPrincipal.Claims);
        
        return new ClaimsPrincipal(new ClaimsIdentity(claims, incomingPrincipal.Identity?.AuthenticationType,
            ClaimTypes.NameIdentifier, ClaimTypes.Role)
        {
            BootstrapContext = ((ClaimsIdentity)incomingPrincipal.Identity!)?.BootstrapContext
        });
    }

    /// <summary>
    /// Returns the SAML2 claims required for logout.
    /// </summary>
    /// <param name="principal">Claims principal object</param>
    private static IEnumerable<Claim> GetSaml2LogoutClaims(ClaimsPrincipal principal)
    {
        yield return GetClaim(principal, Saml2ClaimTypes.NameId);
        yield return GetClaim(principal, Saml2ClaimTypes.NameIdFormat);
        yield return GetClaim(principal, Saml2ClaimTypes.SessionIndex);
    }

    /// <summary>
    /// Gets a specific claim by type.
    /// </summary>
    /// <param name="principal">Claims principal object</param>
    /// <param name="claimType">Saml2 Claim Types</param>
    private static Claim GetClaim(ClaimsPrincipal principal, string claimType)
    {
        return (((ClaimsIdentity)principal.Identity!)?.Claims!).FirstOrDefault(c => c.Type == claimType) ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets the value of a specific claim by type.
    /// </summary>
    /// <param name="principal">Claims principal object</param>
    /// <param name="claimType">Saml2 Claim Types</param>
    private static string GetClaimValue(ClaimsPrincipal principal, string claimType)
    {
        var claim = GetClaim(principal, claimType);
        return claim.Value;
    }
}
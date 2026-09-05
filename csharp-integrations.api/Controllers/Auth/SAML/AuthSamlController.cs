using System.Security.Authentication;
using csharp_integrations.core.Auth.SAML;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace csharp_integrations.api.Controllers.Auth.SAML;

/// <summary>
/// Handles service-provider initiated SAML authentication.
/// </summary>
[ApiController]
[Route("Auth/Saml/[controller]")]
public class AuthSamlController(IOptions<Saml2Configuration> configAccessor, IConfiguration configuration) : Controller
{
    private const string RelayStateReturnUrl = "ReturnUrl";
    private readonly Saml2Configuration _config = configAccessor.Value;
    private readonly string[] _allowedReturnOrigins = configuration.GetSection("SAML:AllowedReturnOrigins").Get<string[]>() ?? [];
    
    /// <summary>
    /// Creates an authentication request and returns the identity-provider redirect URL.
    /// </summary>
    /// <param name="returnUrl">Optional local URL to redirect to after a successful sign-in.</param>
    /// <returns>The identity-provider redirect URL.</returns>
    [HttpPost("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string>
            { { RelayStateReturnUrl, GetSafeReturnUrl(returnUrl) } });
        
        return binding.Bind(new Saml2AuthnRequest(_config)).ToActionResult() is RedirectResult redirectResult
            ? Ok(redirectResult.Url)
            : BadRequest();
    }
    
    /// <summary>
    /// Consumes a SAML assertion and redirects the authenticated user to a local URL.
    /// </summary>
    /// <returns>A redirect to the validated return URL.</returns>
    [HttpPost("AssertionConsumerService")]
    [AllowAnonymous]
    public async Task<IActionResult> AssertionConsumerService()
    {
        var binding = new Saml2PostBinding();
        var saml2AuthnResponse = new Saml2AuthnResponse(_config);

        binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);

        if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
        {
            throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
        }

        binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);

        await saml2AuthnResponse.CreateSession(HttpContext, claimsTransform: ClaimsTransform.Transform);

        var relayStateQuery = binding.GetRelayStateQuery();
        var returnUrl = relayStateQuery.TryGetValue(RelayStateReturnUrl, out var value) ? value : null;

        return Redirect(GetSafeReturnUrl(returnUrl));
    }

    /// <summary>
    /// Returns a local or explicitly allowed frontend redirect URL, or the application root when the supplied URL is unsafe.
    /// </summary>
    /// <param name="returnUrl">Untrusted return URL from the request or relay state.</param>
    /// <returns>A validated URL suitable for redirection.</returns>
    private string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return Url.Content("~/");
        }

        if (Url.IsLocalUrl(returnUrl) || IsAllowedFrontendReturnUrl(returnUrl))
        {
            return returnUrl;
        }

        return Url.Content("~/");
    }

    /// <summary>
    /// Determines whether an absolute return URL belongs to an allowed frontend origin.
    /// </summary>
    /// <param name="returnUrl">Untrusted absolute return URL.</param>
    /// <returns><see langword="true"/> when the URL origin is explicitly allowed; otherwise, <see langword="false"/>.</returns>
    private bool IsAllowedFrontendReturnUrl(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri))
        {
            return false;
        }

        return _allowedReturnOrigins.Any(allowedOrigin =>
            Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var allowedUri)
            && Uri.Compare(
                returnUri,
                allowedUri,
                UriComponents.SchemeAndServer,
                UriFormat.Unescaped,
                StringComparison.OrdinalIgnoreCase) == 0);
    }
}

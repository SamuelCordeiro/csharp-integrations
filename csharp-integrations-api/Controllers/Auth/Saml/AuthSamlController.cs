using System.Security.Authentication;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using csharp_integrations_core.Auth.SAML;

namespace csharp_integrations_api.Controllers.Auth.Saml;

[ApiController]
[Route("Auth/Saml/[controller]")]
public class AuthSamlController(IOptions<Saml2Configuration> configAccessor) : Controller
{
    private const string RelayStateReturnUrl = "ReturnUrl";
    private readonly Saml2Configuration _config = configAccessor.Value;

    [HttpPost("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string>
            { { RelayStateReturnUrl, returnUrl ?? Url.Content("~/") } });
        
        return binding.Bind(new Saml2AuthnRequest(_config)).ToActionResult() is RedirectResult redirectResult
            ? Ok(redirectResult.Url)
            : BadRequest();
    }

    [HttpPost("AssertionConsumerService")]
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
        var returnUrl = relayStateQuery.TryGetValue(RelayStateReturnUrl, out var value) ? value : Url.Content("~/");

        return Redirect(returnUrl);
    }
}
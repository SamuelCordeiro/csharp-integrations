using csharp_integrations.core.Auth.Bearer;
using csharp_integrations.core.GlobalResources.Models;
using csharp_integrations.core.GlobalResources.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace csharp_integrations.api.Controllers.Auth.Bearer;

/// <summary>
/// Issues JWT access tokens for the demonstration users.
/// </summary>
[ApiController]
[Route("Auth/Bearer/[controller]")]
public class AuthBearerController(TokenService tokenService) : Controller
{
    /// <summary>
    /// Autentica o usuário e retorna um access token JWT.
    /// </summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<LoginResponse> Login([FromBody] UserLogin model)
    {
        var user = UserRepository.Get(model.Username, model.Password);

        if (user == null) return Unauthorized();

        var token = tokenService.Generate(user, 5);

        return Ok(new LoginResponse
        {
            Username = user.Username,
            AccessToken = token
        });
    }
}

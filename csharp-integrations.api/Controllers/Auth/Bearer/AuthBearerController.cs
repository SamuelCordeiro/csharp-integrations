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
public class AuthBearerController(
    RefreshTokenService refreshTokenService,
    IConfiguration configuration) : Controller
{
    private const string RefreshTokenCookieName = "refresh_token";
    private const string RefreshTokenCookiePath = "/Auth/Bearer/AuthBearer";
    private readonly SameSiteMode _refreshTokenSameSite = configuration.GetValue<bool>("Cors:AllowCredentials")
        ? SameSiteMode.None
        : SameSiteMode.Lax;

    /// <summary>
    /// Authenticates a user and returns a short-lived JWT access token.
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

        var tokenPair = refreshTokenService.CreateTokenPair(user);
        SetRefreshTokenCookie(tokenPair);

        return Ok(CreateLoginResponse(tokenPair));
    }

    /// <summary>
    /// Rotates the refresh token cookie and returns a new access token.
    /// </summary>
    [HttpPost("Refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<LoginResponse> Refresh()
    {
        var refreshResult = refreshTokenService.Refresh(Request.Cookies[RefreshTokenCookieName]);

        if (refreshResult.Status != RefreshTokenRotationStatus.Succeeded || refreshResult.TokenPair is null)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized();
        }

        SetRefreshTokenCookie(refreshResult.TokenPair);

        return Ok(CreateLoginResponse(refreshResult.TokenPair));
    }

    /// <summary>
    /// Revokes the current refresh token family and clears its cookie.
    /// </summary>
    [HttpPost("Logout")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult Logout()
    {
        refreshTokenService.Revoke(Request.Cookies[RefreshTokenCookieName]);
        DeleteRefreshTokenCookie();

        return NoContent();
    }

    private void SetRefreshTokenCookie(TokenPair tokenPair)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            tokenPair.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = _refreshTokenSameSite,
                IsEssential = true,
                Path = RefreshTokenCookiePath,
                Expires = tokenPair.RefreshTokenExpiresAtUtc
            });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = _refreshTokenSameSite,
            IsEssential = true,
            Path = RefreshTokenCookiePath
        });
    }

    private static LoginResponse CreateLoginResponse(TokenPair tokenPair)
    {
        return new LoginResponse
        {
            Username = tokenPair.Username,
            AccessToken = tokenPair.AccessToken,
            ExpiresInSeconds = tokenPair.ExpiresInSeconds
        };
    }
}

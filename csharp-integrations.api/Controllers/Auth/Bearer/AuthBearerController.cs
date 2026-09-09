using csharp_integrations.core.Auth.Bearer;
using csharp_integrations.api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    TokenService tokenService,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
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
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest model)
    {
        var user = await userManager.FindByNameAsync(model.Username);

        if (user is null)
        {
            return Unauthorized();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return Unauthorized();
        }

        var refreshTokenIssue = await refreshTokenService.CreateAsync(user.Id, user.UserName!);
        var roles = await userManager.GetRolesAsync(user);
        SetRefreshTokenCookie(refreshTokenIssue);

        return Ok(CreateLoginResponse(user, roles));
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
    public async Task<ActionResult<LoginResponse>> Refresh()
    {
        var refreshResult = await refreshTokenService.RefreshAsync(Request.Cookies[RefreshTokenCookieName]);

        if (refreshResult.Status != RefreshTokenRotationStatus.Succeeded || refreshResult.RefreshTokenIssue is null)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(refreshResult.RefreshTokenIssue.UserId.ToString());
        if (user is null)
        {
            await refreshTokenService.RevokeAsync(refreshResult.RefreshTokenIssue.RefreshToken);
            DeleteRefreshTokenCookie();
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        SetRefreshTokenCookie(refreshResult.RefreshTokenIssue);

        return Ok(CreateLoginResponse(user, roles));
    }

    /// <summary>
    /// Revokes the current refresh token family and clears its cookie.
    /// </summary>
    [HttpPost("Logout")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Logout()
    {
        await refreshTokenService.RevokeAsync(Request.Cookies[RefreshTokenCookieName]);
        DeleteRefreshTokenCookie();

        return NoContent();
    }

    private void SetRefreshTokenCookie(RefreshTokenIssue refreshTokenIssue)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshTokenIssue.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = _refreshTokenSameSite,
                IsEssential = true,
                Path = RefreshTokenCookiePath,
                Expires = refreshTokenIssue.RefreshTokenExpiresAtUtc
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

    private LoginResponse CreateLoginResponse(
        ApplicationUser user,
        IEnumerable<string> roles)
    {
        return new LoginResponse
        {
            Username = user.UserName!,
            AccessToken = tokenService.GenerateAccessToken(user.Id, user.UserName!, roles),
            ExpiresInSeconds = (int)tokenService.GetAccessTokenLifetime().TotalSeconds
        };
    }
}

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using csharp_integrations.core.GlobalResources.Models;
using Microsoft.Extensions.Configuration;

namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Creates signed JWT access tokens for authenticated users.
/// </summary>
public class TokenService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes the service with application configuration.
    /// </summary>
    /// <param name="configuration">Application configuration containing JWT settings.</param>
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a signed JWT access token for a user.
    /// </summary>
    /// <param name="user">User Object</param>
    /// <param name="minutesToExpire">Token expiration time in minutes</param>
    /// <returns>The serialized JWT access token.</returns>
    public string Generate(User user, double minutesToExpire)
    {
        var handler = new JwtSecurityTokenHandler();

        var apiKey = _configuration["BearerToken:ApiKey"]
                     ?? throw new InvalidOperationException("BearerToken:ApiKey not configured.");
        var issuer = _configuration["BearerToken:Issuer"]
                     ?? throw new InvalidOperationException("BearerToken:Issuer not configured.");
        var audience = _configuration["BearerToken:Audience"]
                       ?? throw new InvalidOperationException("BearerToken:Audience not configured.");
        var key = Encoding.UTF8.GetBytes(apiKey);

        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = GenerateClaims(user),
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddMinutes(minutesToExpire),
            Issuer = issuer,
            Audience = audience
        };

        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);
    }

    /// <summary>
    /// Generates the claims included in an access token.
    /// </summary>
    /// <param name="user">User Object</param>
    /// <returns>An identity containing the user claims.</returns>
    private static ClaimsIdentity GenerateClaims(User user)
    {
        var ci = new ClaimsIdentity();
        ci.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        ci.AddClaim(new Claim(type: "Id", value: user.Id.ToString()));

        return ci;
    }

    /// <summary>
    /// Extracts the user identifier from token claims.
    /// </summary>
    /// <param name="userClaims">User claims</param>
    /// <returns>The numeric user identifier.</returns>
    /// <exception cref="ArgumentNullException">Exception for null claims</exception>
    /// <exception cref="InvalidOperationException">Exception for claims without id</exception>
    /// <exception cref="FormatException">Exception for non-numeric claim id</exception>
    public static int GetUserId(ClaimsPrincipal userClaims)
    {
        if (userClaims == null)
            throw new ArgumentNullException(nameof(userClaims), "User cannot be null.");

        var idClaim = userClaims.FindFirst("Id");
        if (idClaim == null)
            throw new InvalidOperationException("The claim 'Id' was not found for the user.");

        if (!int.TryParse(idClaim.Value, out int userId))
            throw new FormatException("The claim 'Id' does not contain a valid numeric value.");

        return userId;
    }
}

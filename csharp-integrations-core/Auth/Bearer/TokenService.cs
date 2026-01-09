using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using csharp_integrations_core.GlobalResources.Models;

namespace csharp_integrations_core.Auth.Bearer;

public class TokenService
{
    /// <summary>
    /// Generates a JWT token for any user object with a mapping function to Claims.
    /// </summary>
    /// <param name="user">User Object</param>
    /// <param name="minutesToExpire">Token expiration time in minutes</param>
    /// <returns></returns>
    public String Generate(User user, Double minutesToExpire)
    {
        var handler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("APIKEY") ?? string.Empty);

        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = GenerateClaims(user),
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddMinutes(minutesToExpire),
        };

        var token = handler.CreateToken(tokenDescriptor);

        return handler.WriteToken(token);
    }

    /// <summary>
    /// Generates user claims
    /// </summary>
    /// <param name="user">User Object</param>
    /// <returns></returns>
    private static ClaimsIdentity GenerateClaims(User user)
    {
        var ci = new ClaimsIdentity();
        ci.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        ci.AddClaim(new Claim("Id", user.Id.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer));

        return ci;
    }

    /// <summary>
    /// Extract user id from token claims.
    /// </summary>
    /// <param name="userClaims">User claims</param>
    /// <returns></returns>
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

        return !int.TryParse(idClaim.Value, out var userId)
            ? throw new FormatException("The claim 'Id' does not contain a valid numeric value.")
            : userId;
    }
}
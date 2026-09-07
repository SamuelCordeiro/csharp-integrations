using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using csharp_integrations.core.Auth.Bearer;
using csharp_integrations.core.GlobalResources.Models;
using Microsoft.Extensions.Configuration;

namespace csharp_integrations.tests.Unit;

/// <summary>
/// Covers JWT creation and claim extraction without hosting the API.
/// </summary>
public sealed class TokenServiceTests
{
    private const string SigningKey = "unit-test-signing-key-that-is-long-enough-for-hmac-sha256";
    private const string Issuer = "csharp-integrations-unit-tests";
    private const string Audience = "csharp-integrations-unit-tests-client";

    /// <summary>
    /// Verifies that generated tokens contain the configured issuer, audience and user claims.
    /// </summary>
    [Fact]
    public void Generate_AddsConfiguredIssuerAudienceAndUserClaims()
    {
        var service = new TokenService(CreateConfiguration());
        var user = new User { Id = 42, Username = "test-user", Password = "not-used", Role = "employee" };

        var serializedToken = service.Generate(user, 5);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(serializedToken);

        Assert.Equal(Issuer, token.Issuer);
        Assert.Contains(Audience, token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.UniqueName && claim.Value == user.Username);
        Assert.Contains(token.Claims, claim => claim.Type == "Id" && claim.Value == user.Id.ToString());
        Assert.True(token.ValidTo > DateTime.UtcNow);
    }

    /// <summary>
    /// Verifies that a numeric identifier claim is returned as an integer.
    /// </summary>
    [Fact]
    public void GetUserId_WithNumericIdClaim_ReturnsIdentifier()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("Id", "42")]));

        var userId = TokenService.GetUserId(principal);

        Assert.Equal(42, userId);
    }

    /// <summary>
    /// Verifies that a missing identifier claim is rejected explicitly.
    /// </summary>
    [Fact]
    public void GetUserId_WithoutIdClaim_ThrowsInvalidOperationException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<InvalidOperationException>(() => TokenService.GetUserId(principal));
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BearerToken:ApiKey"] = SigningKey,
            ["BearerToken:Issuer"] = Issuer,
            ["BearerToken:Audience"] = Audience
        })
        .Build();
}

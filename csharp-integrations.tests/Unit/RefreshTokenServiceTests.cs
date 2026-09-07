using csharp_integrations.core.Auth.Bearer;
using csharp_integrations.core.GlobalResources.Models;
using Microsoft.Extensions.Configuration;

namespace csharp_integrations.tests.Unit;

/// <summary>
/// Covers refresh token rotation, replay detection, and revocation.
/// </summary>
public sealed class RefreshTokenServiceTests
{
    /// <summary>
    /// Verifies that replaying a rotated token revokes every token in its family.
    /// </summary>
    [Fact]
    public void Refresh_WithReusedToken_RevokesTheTokenFamily()
    {
        var service = CreateService();
        var user = new User { Id = 42, Username = "test-user", Password = "not-used", Role = "employee" };
        var initialPair = service.CreateTokenPair(user);

        var rotation = service.Refresh(initialPair.RefreshToken);
        var replay = service.Refresh(initialPair.RefreshToken);
        var replacementAttempt = service.Refresh(rotation.TokenPair!.RefreshToken);

        Assert.Equal(RefreshTokenRotationStatus.Succeeded, rotation.Status);
        Assert.NotNull(rotation.TokenPair);
        Assert.NotEqual(initialPair.RefreshToken, rotation.TokenPair.RefreshToken);
        Assert.Equal(RefreshTokenRotationStatus.Reused, replay.Status);
        Assert.Equal(RefreshTokenRotationStatus.Reused, replacementAttempt.Status);
    }

    /// <summary>
    /// Verifies that revoking a refresh token prevents future rotations.
    /// </summary>
    [Fact]
    public void Revoke_WithActiveToken_PreventsFutureRotation()
    {
        var service = CreateService();
        var user = new User { Id = 42, Username = "test-user", Password = "not-used", Role = "employee" };
        var tokenPair = service.CreateTokenPair(user);

        var revoked = service.Revoke(tokenPair.RefreshToken);
        var refreshResult = service.Refresh(tokenPair.RefreshToken);

        Assert.True(revoked);
        Assert.Equal(RefreshTokenRotationStatus.Reused, refreshResult.Status);
    }

    private static RefreshTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BearerToken:ApiKey"] = "unit-test-signing-key-that-is-long-enough-for-hmac-sha256",
                ["BearerToken:Issuer"] = "csharp-integrations-unit-tests",
                ["BearerToken:Audience"] = "csharp-integrations-unit-tests-client",
                ["BearerToken:AccessTokenMinutes"] = "5",
                ["BearerToken:RefreshTokenDays"] = "7"
            })
            .Build();

        return new RefreshTokenService(
            new TokenService(configuration),
            new InMemoryRefreshTokenStore(),
            configuration);
    }
}

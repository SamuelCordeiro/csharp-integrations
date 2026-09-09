using csharp_integrations.core.Auth.Bearer;
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
    public async Task RefreshAsync_WithReusedToken_RevokesTheTokenFamily()
    {
        var service = CreateService();
        var initialIssue = await service.CreateAsync(42, "test-user");

        var rotation = await service.RefreshAsync(initialIssue.RefreshToken);
        var replay = await service.RefreshAsync(initialIssue.RefreshToken);
        var replacementAttempt = await service.RefreshAsync(rotation.RefreshTokenIssue!.RefreshToken);

        Assert.Equal(RefreshTokenRotationStatus.Succeeded, rotation.Status);
        Assert.NotNull(rotation.RefreshTokenIssue);
        Assert.NotEqual(initialIssue.RefreshToken, rotation.RefreshTokenIssue.RefreshToken);
        Assert.Equal(42, rotation.RefreshTokenIssue.UserId);
        Assert.Equal(RefreshTokenRotationStatus.Reused, replay.Status);
        Assert.Equal(RefreshTokenRotationStatus.Reused, replacementAttempt.Status);
    }

    /// <summary>
    /// Verifies that revoking a refresh token prevents future rotations.
    /// </summary>
    [Fact]
    public async Task RevokeAsync_WithActiveToken_PreventsFutureRotation()
    {
        var service = CreateService();
        var refreshTokenIssue = await service.CreateAsync(42, "test-user");

        var revoked = await service.RevokeAsync(refreshTokenIssue.RefreshToken);
        var refreshResult = await service.RefreshAsync(refreshTokenIssue.RefreshToken);

        Assert.True(revoked);
        Assert.Equal(RefreshTokenRotationStatus.Reused, refreshResult.Status);
    }

    private static RefreshTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BearerToken:RefreshTokenDays"] = "7"
            })
            .Build();

        return new RefreshTokenService(new InMemoryRefreshTokenStore(), configuration);
    }
}

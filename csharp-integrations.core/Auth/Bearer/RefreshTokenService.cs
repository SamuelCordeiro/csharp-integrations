using System.Security.Cryptography;
using System.Text;
using csharp_integrations.core.GlobalResources.Models;
using Microsoft.Extensions.Configuration;

namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Issues, rotates, and revokes opaque refresh tokens.
/// </summary>
public sealed class RefreshTokenService(
    TokenService tokenService,
    InMemoryRefreshTokenStore refreshTokenStore,
    IConfiguration configuration)
{
    /// <summary>
    /// Creates a short-lived access token and a refresh token for a user.
    /// </summary>
    /// <param name="user">Authenticated user.</param>
    /// <returns>The access token and raw refresh token for delivery to the client.</returns>
    public TokenPair CreateTokenPair(User user)
    {
        var refreshToken = CreateRefreshToken();
        var refreshTokenRecord = CreateRecord(user.Id, user.Username, Guid.NewGuid(), refreshToken);
        refreshTokenStore.Add(refreshTokenRecord);

        return CreateTokenPair(user.Id, user.Username, refreshToken, refreshTokenRecord.ExpiresAtUtc);
    }

    /// <summary>
    /// Rotates a valid refresh token and returns a new token pair.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token supplied by the client.</param>
    /// <returns>The rotation result and replacement token pair when successful.</returns>
    public RefreshTokenRefreshResult Refresh(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new RefreshTokenRefreshResult { Status = RefreshTokenRotationStatus.Invalid };
        }

        var replacementToken = CreateRefreshToken();
        var now = DateTime.UtcNow;
        var rotationResult = refreshTokenStore.Rotate(
            HashToken(refreshToken),
            currentToken => CreateRecord(
                currentToken.UserId,
                currentToken.Username,
                currentToken.FamilyId,
                replacementToken),
            now);

        if (rotationResult.Status != RefreshTokenRotationStatus.Succeeded || rotationResult.RefreshToken is null)
        {
            return new RefreshTokenRefreshResult { Status = rotationResult.Status };
        }

        var tokenPair = CreateTokenPair(
            rotationResult.RefreshToken.UserId,
            rotationResult.RefreshToken.Username,
            replacementToken,
            rotationResult.RefreshToken.ExpiresAtUtc);

        return new RefreshTokenRefreshResult
        {
            Status = RefreshTokenRotationStatus.Succeeded,
            TokenPair = tokenPair
        };
    }

    /// <summary>
    /// Revokes every refresh token in the family of the supplied token.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token supplied by the client.</param>
    /// <returns><see langword="true"/> when a token family was revoked.</returns>
    public bool Revoke(string? refreshToken)
    {
        return !string.IsNullOrWhiteSpace(refreshToken)
               && refreshTokenStore.RevokeFamily(HashToken(refreshToken), DateTime.UtcNow);
    }

    private TokenPair CreateTokenPair(
        int userId,
        string username,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        return new TokenPair
        {
            Username = username,
            AccessToken = tokenService.GenerateAccessToken(userId, username),
            RefreshToken = refreshToken,
            ExpiresInSeconds = (int)tokenService.GetAccessTokenLifetime().TotalSeconds,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }

    private RefreshTokenRecord CreateRecord(int userId, string username, Guid familyId, string refreshToken)
    {
        return new RefreshTokenRecord
        {
            TokenHash = HashToken(refreshToken),
            FamilyId = familyId,
            UserId = userId,
            Username = username,
            ExpiresAtUtc = DateTime.UtcNow.Add(GetRefreshTokenLifetime())
        };
    }

    private TimeSpan GetRefreshTokenLifetime()
    {
        var days = configuration.GetValue<double?>("BearerToken:RefreshTokenDays") ?? 7;

        if (days <= 0)
        {
            throw new InvalidOperationException("BearerToken:RefreshTokenDays must be greater than zero.");
        }

        return TimeSpan.FromDays(days);
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}

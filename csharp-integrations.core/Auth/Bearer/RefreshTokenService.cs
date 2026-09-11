using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Issues, rotates, and revokes opaque refresh tokens.
/// </summary>
public sealed class RefreshTokenService(
    IRefreshTokenStore refreshTokenStore,
    IConfiguration configuration)
{
    /// <summary>
    /// Creates a refresh token for the authenticated user.
    /// </summary>
    /// <param name="userId">Authenticated user identifier.</param>
    /// <param name="username">Authenticated username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw refresh token and its metadata.</returns>
    public async Task<RefreshTokenIssue> CreateAsync(int userId, string username, CancellationToken cancellationToken = default)
    {
        var refreshToken = CreateRefreshToken();
        var refreshTokenRecord = CreateRecord(userId, username, Guid.NewGuid(), refreshToken);
        await refreshTokenStore.AddAsync(refreshTokenRecord, cancellationToken);

        return CreateRefreshTokenIssue(userId, username, refreshToken, refreshTokenRecord.ExpiresAtUtc);
    }

    /// <summary>
    /// Rotates a valid refresh token and returns a new token pair.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token supplied by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rotation result and replacement token pair when successful.</returns>
    public async Task<RefreshTokenRefreshResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new RefreshTokenRefreshResult { Status = RefreshTokenRotationStatus.Invalid };
        }

        var replacementToken = CreateRefreshToken();
        var now = DateTime.UtcNow;
        var rotationResult = await refreshTokenStore.RotateAsync(
            HashToken(refreshToken),
            currentToken => CreateRecord(
                currentToken.UserId,
                currentToken.Username,
                currentToken.FamilyId,
                replacementToken),
            now,
            cancellationToken);

        if (rotationResult.Status != RefreshTokenRotationStatus.Succeeded || rotationResult.RefreshToken is null)
        {
            return new RefreshTokenRefreshResult { Status = rotationResult.Status };
        }

        var refreshTokenIssue = CreateRefreshTokenIssue(
            rotationResult.RefreshToken.UserId,
            rotationResult.RefreshToken.Username,
            replacementToken,
            rotationResult.RefreshToken.ExpiresAtUtc);

        return new RefreshTokenRefreshResult
        {
            Status = RefreshTokenRotationStatus.Succeeded,
            RefreshTokenIssue = refreshTokenIssue
        };
    }

    /// <summary>
    /// Revokes every refresh token in the family of the supplied token.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token supplied by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a token family was revoked.</returns>
    public async Task<bool> RevokeAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        return !string.IsNullOrWhiteSpace(refreshToken)
               && await refreshTokenStore.RevokeFamilyAsync(HashToken(refreshToken), DateTime.UtcNow, cancellationToken);
    }

    /// <summary>
    /// Revokes all refresh tokens issued for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task RevokeUserTokensAsync(int userId, CancellationToken cancellationToken = default)
    {
        return refreshTokenStore.RevokeUserTokensAsync(userId, DateTime.UtcNow, cancellationToken);
    }

    private RefreshTokenIssue CreateRefreshTokenIssue(
        int userId,
        string username,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        return new RefreshTokenIssue
        {
            UserId = userId,
            Username = username,
            RefreshToken = refreshToken,
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

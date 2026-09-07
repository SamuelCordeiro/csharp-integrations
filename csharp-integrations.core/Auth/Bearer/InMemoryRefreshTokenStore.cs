namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Stores hashed refresh tokens for the demonstration application.
/// </summary>
public sealed class InMemoryRefreshTokenStore
{
    private readonly Dictionary<string, RefreshTokenRecord> _tokens = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    /// <summary>
    /// Stores a new refresh token record.
    /// </summary>
    /// <param name="refreshToken">Hashed refresh token record.</param>
    public void Add(RefreshTokenRecord refreshToken)
    {
        lock (_lock)
        {
            RemoveExpiredTokens(DateTime.UtcNow);
            _tokens.Add(refreshToken.TokenHash, refreshToken);
        }
    }

    /// <summary>
    /// Atomically revokes a token and replaces it with a new token in the same family.
    /// </summary>
    /// <param name="tokenHash">Hash of the token presented by the client.</param>
    /// <param name="createReplacement">Creates the replacement record from the active token.</param>
    /// <param name="now">Current UTC date.</param>
    /// <returns>The result of the rotation attempt.</returns>
    public RefreshTokenRotationResult Rotate(
        string tokenHash,
        Func<RefreshTokenRecord, RefreshTokenRecord> createReplacement,
        DateTime now)
    {
        lock (_lock)
        {
            RemoveExpiredTokens(now);

            if (!_tokens.TryGetValue(tokenHash, out var currentToken) || currentToken.ExpiresAtUtc <= now)
            {
                return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Invalid };
            }

            if (currentToken.RevokedAtUtc is not null)
            {
                RevokeFamily(currentToken.FamilyId, now);
                return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Reused };
            }

            var replacement = createReplacement(currentToken);
            currentToken.RevokedAtUtc = now;
            currentToken.ReplacedByTokenHash = replacement.TokenHash;
            _tokens.Add(replacement.TokenHash, replacement);

            return new RefreshTokenRotationResult
            {
                Status = RefreshTokenRotationStatus.Succeeded,
                RefreshToken = replacement
            };
        }
    }

    /// <summary>
    /// Revokes the token family associated with the presented token.
    /// </summary>
    /// <param name="tokenHash">Hash of the token presented by the client.</param>
    /// <param name="now">Current UTC date.</param>
    /// <returns><see langword="true"/> when a token family was revoked.</returns>
    public bool RevokeFamily(string tokenHash, DateTime now)
    {
        lock (_lock)
        {
            RemoveExpiredTokens(now);

            if (!_tokens.TryGetValue(tokenHash, out var refreshToken))
            {
                return false;
            }

            RevokeFamily(refreshToken.FamilyId, now);
            return true;
        }
    }

    private void RevokeFamily(Guid familyId, DateTime now)
    {
        foreach (var refreshToken in _tokens.Values.Where(token => token.FamilyId == familyId))
        {
            refreshToken.RevokedAtUtc ??= now;
        }
    }

    private void RemoveExpiredTokens(DateTime now)
    {
        var expiredTokenHashes = _tokens
            .Where(pair => pair.Value.ExpiresAtUtc <= now)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var tokenHash in expiredTokenHashes)
        {
            _tokens.Remove(tokenHash);
        }
    }
}

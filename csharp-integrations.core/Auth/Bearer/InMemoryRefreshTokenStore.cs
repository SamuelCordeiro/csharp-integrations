namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Stores hashed refresh tokens for the demonstration application.
/// </summary>
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<string, RefreshTokenRecord> _tokens = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    /// <summary>
    /// Stores a new refresh token record.
    /// </summary>
    /// <param name="refreshToken">Hashed refresh token record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task AddAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            RemoveExpiredTokens(DateTime.UtcNow);
            _tokens.Add(refreshToken.TokenHash, refreshToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Atomically revokes a token and replaces it with a new token in the same family.
    /// </summary>
    /// <param name="tokenHash">Hash of the token presented by the client.</param>
    /// <param name="createReplacement">Creates the replacement record from the active token.</param>
    /// <param name="now">Current UTC date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the rotation attempt.</returns>
    public Task<RefreshTokenRotationResult> RotateAsync(
        string tokenHash,
        Func<RefreshTokenRecord, RefreshTokenRecord> createReplacement,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            RemoveExpiredTokens(now);

            if (!_tokens.TryGetValue(tokenHash, out var currentToken) || currentToken.ExpiresAtUtc <= now)
            {
                return Task.FromResult(new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Invalid });
            }

            if (currentToken.RevokedAtUtc is not null)
            {
                RevokeFamily(currentToken.FamilyId, now);
                return Task.FromResult(new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Reused });
            }

            var replacement = createReplacement(currentToken);
            currentToken.RevokedAtUtc = now;
            currentToken.ReplacedByTokenHash = replacement.TokenHash;
            _tokens.Add(replacement.TokenHash, replacement);

            return Task.FromResult(new RefreshTokenRotationResult
            {
                Status = RefreshTokenRotationStatus.Succeeded,
                RefreshToken = replacement
            });
        }
    }

    /// <summary>
    /// Revokes the token family associated with the presented token.
    /// </summary>
    /// <param name="tokenHash">Hash of the token presented by the client.</param>
    /// <param name="now">Current UTC date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a token family was revoked.</returns>
    public Task<bool> RevokeFamilyAsync(string tokenHash, DateTime now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            RemoveExpiredTokens(now);

            if (!_tokens.TryGetValue(tokenHash, out var refreshToken))
            {
                return Task.FromResult(false);
            }

            RevokeFamily(refreshToken.FamilyId, now);
            return Task.FromResult(true);
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

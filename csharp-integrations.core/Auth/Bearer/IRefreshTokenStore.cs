namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Defines persistent operations for hashed refresh tokens.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Stores a new refresh token record.
    /// </summary>
    Task AddAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically rotates a refresh token.
    /// </summary>
    Task<RefreshTokenRotationResult> RotateAsync(
        string tokenHash,
        Func<RefreshTokenRecord, RefreshTokenRecord> createReplacement,
        DateTime now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens in the family associated with a token hash.
    /// </summary>
    Task<bool> RevokeFamilyAsync(
        string tokenHash,
        DateTime now,
        CancellationToken cancellationToken = default);
}

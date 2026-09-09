using csharp_integrations.core.Auth.Bearer;
using Microsoft.EntityFrameworkCore;

namespace csharp_integrations.api.Data;

/// <summary>
/// Persists hashed refresh tokens using Entity Framework Core.
/// </summary>
public sealed class EfRefreshTokenStore(ApplicationDbContext database) : IRefreshTokenStore
{
    /// <inheritdoc />
    public async Task AddAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken = default)
    {
        database.RefreshTokens.Add(ToEntity(refreshToken));
        await database.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshTokenRotationResult> RotateAsync(
        string tokenHash,
        Func<RefreshTokenRecord, RefreshTokenRecord> createReplacement,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var currentToken = await database.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (currentToken is null || currentToken.ExpiresAtUtc <= now)
        {
            return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Invalid };
        }

        if (currentToken.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(tokenHash, now, cancellationToken);
            return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Reused };
        }

        var replacement = createReplacement(ToRecord(currentToken));
        currentToken.RevokedAtUtc = now;
        currentToken.ReplacedByTokenHash = replacement.TokenHash;
        database.RefreshTokens.Add(ToEntity(replacement));

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await RevokeFamilyAsync(tokenHash, now, cancellationToken);
            return new RefreshTokenRotationResult { Status = RefreshTokenRotationStatus.Reused };
        }

        return new RefreshTokenRotationResult
        {
            Status = RefreshTokenRotationStatus.Succeeded,
            RefreshToken = replacement
        };
    }

    /// <inheritdoc />
    public async Task<bool> RevokeFamilyAsync(string tokenHash, DateTime now, CancellationToken cancellationToken = default)
    {
        var refreshToken = await database.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return false;
        }

        await database.RefreshTokens
            .Where(token => token.FamilyId == refreshToken.FamilyId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(token => token.RevokedAtUtc, now),
                cancellationToken);

        return true;
    }

    private static RefreshTokenEntity ToEntity(RefreshTokenRecord token) => new()
    {
        TokenHash = token.TokenHash,
        FamilyId = token.FamilyId,
        UserId = token.UserId,
        ExpiresAtUtc = token.ExpiresAtUtc,
        RevokedAtUtc = token.RevokedAtUtc,
        ReplacedByTokenHash = token.ReplacedByTokenHash,
    };

    private static RefreshTokenRecord ToRecord(RefreshTokenEntity token) => new()
    {
        TokenHash = token.TokenHash,
        FamilyId = token.FamilyId,
        UserId = token.UserId,
        Username = token.User?.UserName ?? throw new InvalidOperationException("User name is required."),
        ExpiresAtUtc = token.ExpiresAtUtc,
        RevokedAtUtc = token.RevokedAtUtc,
        ReplacedByTokenHash = token.ReplacedByTokenHash
    };
}

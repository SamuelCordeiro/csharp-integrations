using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace csharp_integrations.api.Data;

/// <summary>
/// Provides persistence for Identity users, roles, and refresh tokens.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    /// <summary>
    /// Gets persisted refresh tokens.
    /// </summary>
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    /// <summary>
    /// Gets the persisted active password policy.
    /// </summary>
    public DbSet<PasswordPolicy> PasswordPolicies => Set<PasswordPolicy>();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetUserTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetUserTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(user => user.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(token => token.TokenHash);
            entity.Property(token => token.TokenHash).HasMaxLength(64);
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property(token => token.RevokedAtUtc).IsConcurrencyToken();
            entity.HasIndex(token => new { token.UserId, token.FamilyId });
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasswordPolicy>(entity =>
        {
            entity.HasKey(policy => policy.Id);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_PasswordPolicies_MinimumLength", "MinimumLength >= 8");
                table.HasCheckConstraint("CK_PasswordPolicies_RequiredUniqueCharacters", "RequiredUniqueCharacters >= 1");
                table.HasCheckConstraint("CK_PasswordPolicies_MaxFailedAccessAttempts", "MaxFailedAccessAttempts >= 1");
                table.HasCheckConstraint("CK_PasswordPolicies_LockoutDurationMinutes", "LockoutDurationMinutes >= 1");
            });
        });
    }

    private void SetUserTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}

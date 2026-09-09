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
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
}

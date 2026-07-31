using EnterpriseFramework.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Auth.Persistence;

/// <summary>
/// EF Core context OWNED by the Auth module.
///
/// Module-owned persistence is part of the Module Contract: the module's
/// tables live in their own PostgreSQL schema ("auth") with their own
/// migration history, so installing or removing the module never touches
/// another module's data. The host's AppDbContext stays module-agnostic.
/// </summary>
public sealed class AuthDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "auth";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options) { }

    /// <summary>Registered accounts.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Issued refresh tokens (hashes only).</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Configures tables, indexes and constraints of the auth schema.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            // Uniqueness is enforced on the normalized form: the application
            // checks first for a friendly error, the index wins every race.
            user.HasIndex(u => u.NormalizedEmail).IsUnique();
            user.Property(u => u.Email).HasMaxLength(320);
            user.Property(u => u.NormalizedEmail).HasMaxLength(320);
            user.Property(u => u.DisplayName).HasMaxLength(200);
            user.Property(u => u.PasswordHash).HasMaxLength(500);
            user.Property(u => u.Roles).HasColumnType("text[]");
            user.Ignore(u => u.DomainEvents);
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.ToTable("refresh_tokens");
            token.HasKey(t => t.Id);
            // Lookup path on every refresh call.
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.UserId);
            token.Property(t => t.TokenHash).HasMaxLength(88);
            token.Property(t => t.ReplacedByTokenHash).HasMaxLength(88);
            token
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            token.Ignore(t => t.DomainEvents);
        });
    }
}

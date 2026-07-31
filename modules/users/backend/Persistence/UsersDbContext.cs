using EnterpriseFramework.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Users.Persistence;

/// <summary>
/// EF Core context OWNED by the Users module (schema "users").
/// </summary>
public sealed class UsersDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "users";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options) { }

    /// <summary>Account projections maintained from Auth events.</summary>
    public DbSet<UserProfile> Profiles => Set<UserProfile>();

    /// <summary>
    /// Configures tables and indexes of the users schema.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UserProfile>(profile =>
        {
            profile.ToTable("profiles");
            // The key IS the Auth account id: the projection cannot drift into
            // having its own identity.
            profile.HasKey(p => p.Id);
            profile.Property(p => p.Id).ValueGeneratedNever();
            profile.HasIndex(p => p.Email).IsUnique();
            profile.Property(p => p.Email).HasMaxLength(320);
            profile.Property(p => p.DisplayName).HasMaxLength(200);
            profile.Property(p => p.JobTitle).HasMaxLength(200);
            profile.Ignore(p => p.DomainEvents);
        });
    }
}

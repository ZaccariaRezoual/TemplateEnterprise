using EnterpriseFramework.Modules.Authorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Authorization.Persistence;

/// <summary>
/// EF Core context OWNED by the Authorization module (schema "authz").
///
/// Note the absence of a users table: user accounts belong to the Auth
/// module, and this schema references them by id only.
/// </summary>
public sealed class AuthorizationDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "authz";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options)
        : base(options) { }

    /// <summary>Defined roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Role assignments.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>
    /// Configures tables, indexes and constraints of the authz schema.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Role>(role =>
        {
            role.ToTable("roles");
            role.HasKey(r => r.Id);
            role.HasIndex(r => r.Name).IsUnique();
            role.Property(r => r.Name).HasMaxLength(100);
            role.Property(r => r.Description).HasMaxLength(500);
            // Permissions are a value list, not an entity: they are a closed
            // catalogue defined in code, so a join table would add joins
            // without adding meaning.
            role.Property<List<string>>("_permissions")
                .HasColumnName("permissions")
                .HasColumnType("text[]");
            role.Ignore(r => r.Permissions);
            role.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<UserRole>(userRole =>
        {
            userRole.ToTable("user_roles");
            userRole.HasKey(ur => ur.Id);
            // One assignment per (user, role): granting twice is a no-op, and
            // the index makes that a database guarantee rather than a hope.
            userRole.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            userRole
                .HasOne<Role>()
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            userRole.Ignore(ur => ur.DomainEvents);
        });
    }
}

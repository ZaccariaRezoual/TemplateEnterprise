using EnterpriseFramework.Modules.Settings.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Settings.Persistence;

/// <summary>
/// EF Core context OWNED by the Settings module (schema "settings").
/// </summary>
public sealed class SettingsDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "settings";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public SettingsDbContext(DbContextOptions<SettingsDbContext> options)
        : base(options) { }

    /// <summary>Stored setting values (global and per-user overrides).</summary>
    public DbSet<Setting> Settings => Set<Setting>();

    /// <summary>
    /// Configures the settings table.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Setting>(setting =>
        {
            setting.ToTable("values");
            setting.HasKey(s => s.Id);
            // One value per (key, scope): the index makes "set" idempotent at
            // the database level, not just in the handler.
            setting.HasIndex(s => new { s.Key, s.UserId }).IsUnique();
            setting.Property(s => s.Key).HasMaxLength(200);
            setting.Property(s => s.Value).HasMaxLength(4000);
            setting.Ignore(s => s.DomainEvents);
        });
    }
}

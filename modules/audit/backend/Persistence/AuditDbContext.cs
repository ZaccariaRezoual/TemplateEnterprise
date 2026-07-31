using EnterpriseFramework.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Audit.Persistence;

/// <summary>
/// EF Core context OWNED by the Audit module (schema "audit").
/// </summary>
public sealed class AuditDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "audit";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options) { }

    /// <summary>Recorded actions.</summary>
    public DbSet<AuditEntry> Entries => Set<AuditEntry>();

    /// <summary>
    /// Configures the audit table and the indexes its queries rely on.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AuditEntry>(entry =>
        {
            entry.ToTable("entries");
            entry.HasKey(e => e.Id);
            // The trail is read newest-first and filtered by actor; both
            // queries would degrade badly on a growing table without these.
            entry.HasIndex(e => e.OccurredAtUtc).IsDescending();
            entry.HasIndex(e => e.UserId);
            entry.Property(e => e.Action).HasMaxLength(200);
            entry.Property(e => e.CorrelationId).HasMaxLength(64);
            entry.Property(e => e.Source).HasConversion<string>().HasMaxLength(20);
            entry.Ignore(e => e.DomainEvents);
        });
    }
}

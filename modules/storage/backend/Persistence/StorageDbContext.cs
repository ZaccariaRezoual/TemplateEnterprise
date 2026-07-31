using EnterpriseFramework.Modules.Storage.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Storage.Persistence;

/// <summary>
/// EF Core context OWNED by the Storage module (schema "storage").
/// </summary>
public sealed class StorageDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "storage";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public StorageDbContext(DbContextOptions<StorageDbContext> options)
        : base(options) { }

    /// <summary>Metadata of uploaded files.</summary>
    public DbSet<StoredFile> Files => Set<StoredFile>();

    /// <summary>
    /// Configures the files table.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<StoredFile>(file =>
        {
            file.ToTable("files");
            file.HasKey(f => f.Id);
            file.HasIndex(f => f.UploadedByUserId);
            file.Property(f => f.FileName).HasMaxLength(400);
            file.Property(f => f.ContentType).HasMaxLength(200);
            file.Property(f => f.StorageKey).HasMaxLength(200);
            file.Ignore(f => f.DomainEvents);
        });
    }
}

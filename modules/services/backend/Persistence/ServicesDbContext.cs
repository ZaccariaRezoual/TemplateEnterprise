using EnterpriseFramework.Modules.Services.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Services.Persistence;

/// <summary>
/// EF Core context OWNED by the Services module (schema "services").
///
/// It holds the catalogue and the image associations. Note what it does NOT
/// hold: any foreign key to another module's schema. An image points at a
/// Storage file by identifier, and the database is not asked to enforce that
/// link — the two modules must remain separately installable.
/// </summary>
public sealed class ServicesDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "services";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public ServicesDbContext(DbContextOptions<ServicesDbContext> options)
        : base(options) { }

    /// <summary>The catalogue, drafts and archived entries included.</summary>
    public DbSet<Service> Services => Set<Service>();

    /// <summary>Images attached to services.</summary>
    public DbSet<ServiceImage> Images => Set<ServiceImage>();

    /// <summary>
    /// Configures the tables and indexes of the services schema.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Identifiers are minted by the domain, never by the database.
        // Declaring it matters for the CHILD rows: when EF discovers an image
        // through the navigation of a loaded service, it infers "new or
        // existing?" from the key, and a store-generated key that already has
        // a value reads as "existing" — producing an UPDATE against a row
        // that was never inserted.
        modelBuilder.Entity<Service>(service =>
        {
            service.ToTable("services");
            service.HasKey(s => s.Id);
            service.Property(s => s.Id).ValueGeneratedNever();

            // Unique across the whole catalogue, archived entries included: a
            // slug that comes back to life would resurrect an address whose
            // old content is still linked from somewhere.
            service.HasIndex(s => s.Slug).IsUnique();
            service.Property(s => s.Slug).HasMaxLength(Slug.MaxLength);

            service.Property(s => s.Title).HasMaxLength(200);
            service.Property(s => s.ShortDescription).HasMaxLength(300);
            service.Property(s => s.Description).HasMaxLength(8000);
            // Money is never a float: 18,2 keeps the cents exact.
            service.Property(s => s.Price).HasPrecision(18, 2);
            service.Property(s => s.Currency).HasMaxLength(3);

            // The showcase reads exactly this: published, not archived, in
            // order. Indexing the shape of that query is what keeps the
            // public page cheap as the catalogue grows.
            service.HasIndex(s => new { s.IsPublished, s.IsArchived, s.SortOrder });

            service
                .HasMany(s => s.Images)
                .WithOne()
                .HasForeignKey(image => image.ServiceId)
                // Detaching images with their service: the row is a pure
                // association, worthless without the service it describes.
                // The FILE itself survives — deleting it is Storage's call.
                .OnDelete(DeleteBehavior.Cascade);

            service.Navigation(s => s.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
            service.Ignore(s => s.DomainEvents);
            service.Ignore(s => s.AcceptsBookings);
        });

        modelBuilder.Entity<ServiceImage>(image =>
        {
            image.ToTable("service_images");
            image.HasKey(i => i.Id);
            image.Property(i => i.Id).ValueGeneratedNever();
            image.HasIndex(i => new { i.ServiceId, i.SortOrder });
            image.Property(i => i.AltText).HasMaxLength(300);
            image.Ignore(i => i.DomainEvents);
        });
    }
}

using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Infrastructure.Persistence;

/// <summary>
/// EF Core database context of the framework (PostgreSQL).
///
/// Responsibilities:
/// - Applies every <c>IEntityTypeConfiguration</c> found in this assembly.
/// - Will host the entity sets contributed by modules (from Fase 4 onward).
///
/// Entities are NEVER exposed outside the API: endpoints return DTO/Contracts
/// only (architecture rule).
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes the context with the options configured by the composition root.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    /// <summary>
    /// Applies all entity configurations declared in the Infrastructure assembly.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

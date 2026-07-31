using EnterpriseFramework.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Notifications.Persistence;

/// <summary>
/// EF Core context OWNED by the Notifications module (schema "notifications").
/// </summary>
public sealed class NotificationsDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "notifications";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options) { }

    /// <summary>Stored notifications.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Configures the notifications table and the index its queries need.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Notification>(notification =>
        {
            notification.ToTable("notifications");
            notification.HasKey(n => n.Id);
            // Every query is "this user's, newest first" — the composite index
            // serves both the list and the unread badge.
            notification.HasIndex(n => new { n.UserId, n.CreatedAtUtc }).IsDescending(false, true);
            notification.Property(n => n.Title).HasMaxLength(200);
            notification.Property(n => n.Body).HasMaxLength(2000);
            notification.Property(n => n.Link).HasMaxLength(500);
            notification.Property(n => n.Level).HasConversion<string>().HasMaxLength(20);
            notification.Ignore(n => n.IsUnread);
            notification.Ignore(n => n.DomainEvents);
        });
    }
}

using EnterpriseFramework.Modules.Appointments.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Appointments.Persistence;

/// <summary>
/// EF Core context OWNED by the Appointments module (schema "appointments").
///
/// It holds the bookings, the availability rules and the two projections this
/// module maintains from other modules' events. Note what it does NOT hold:
/// a foreign key to the services or the accounts tables. Those live in other
/// schemas, and the projections exist precisely so the database is never
/// asked to join across a module boundary.
/// </summary>
public sealed class AppointmentsDbContext : DbContext
{
    /// <summary>PostgreSQL schema holding every table of this module.</summary>
    public const string Schema = "appointments";

    /// <summary>
    /// Name of the exclusion constraint that makes double booking impossible.
    /// Referenced by the handlers, which turn its violation into an answer a
    /// human can act on.
    /// </summary>
    public const string NoOverlapConstraint = "appointments_no_overlap";

    /// <summary>
    /// Initializes the context with the options configured by the module.
    /// </summary>
    /// <param name="options">EF Core options (provider, connection string).</param>
    public AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options)
        : base(options) { }

    /// <summary>Bookings, in every state.</summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <summary>What happened to each booking.</summary>
    public DbSet<AppointmentHistoryEntry> History => Set<AppointmentHistoryEntry>();

    /// <summary>Reminders already claimed; the unique index is the point.</summary>
    public DbSet<AppointmentReminder> Reminders => Set<AppointmentReminder>();

    /// <summary>Recurring weekly openings.</summary>
    public DbSet<AvailabilityRule> Rules => Set<AvailabilityRule>();

    /// <summary>Closures and extraordinary openings.</summary>
    public DbSet<AvailabilityOverride> Exceptions => Set<AvailabilityOverride>();

    /// <summary>Bookable services, projected from the Services module.</summary>
    public DbSet<ServiceProjection> Services => Set<ServiceProjection>();

    /// <summary>Customers, projected from the Auth module.</summary>
    public DbSet<CustomerProjection> Customers => Set<CustomerProjection>();

    /// <summary>
    /// Configures the tables and indexes of the appointments schema.
    /// </summary>
    /// <param name="modelBuilder">EF Core model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Every identifier in this module is minted by the domain, never by
        // the database. Saying so is not cosmetic: for a CHILD discovered
        // through a navigation — a history entry added to a loaded
        // appointment — EF infers "new or existing?" from the key, and a
        // store-generated key that already has a value reads as "existing".
        // The result is an UPDATE against a row that does not exist, which
        // surfaces as a concurrency exception on a perfectly ordinary save.
        modelBuilder.Entity<Appointment>(appointment =>
        {
            appointment.ToTable("appointments");
            appointment.HasKey(a => a.Id);
            appointment.Property(a => a.Id).ValueGeneratedNever();

            // Stored as its NAME. An enum that leaves this process as an
            // integer means inserting a value in the middle silently
            // rewrites the meaning of every row already saved.
            appointment.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            appointment.Property(a => a.ContactPhone).HasMaxLength(40);
            appointment.Property(a => a.CustomerNote).HasMaxLength(2000);
            appointment.Property(a => a.AdminNote).HasMaxLength(2000);
            appointment.Property(a => a.CancellationReason).HasMaxLength(300);

            // The calendar reads a date range; "my appointments" reads one
            // customer. Both are the shape of a query that runs on every page
            // load of their screen.
            appointment.HasIndex(a => new { a.StartUtc, a.Status });
            appointment.HasIndex(a => new { a.CustomerUserId, a.StartUtc });
            appointment.HasIndex(a => a.ServiceId);

            appointment
                .HasMany(a => a.History)
                .WithOne()
                .HasForeignKey(entry => entry.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            appointment.Navigation(a => a.History).UsePropertyAccessMode(PropertyAccessMode.Field);
            appointment.Ignore(a => a.DomainEvents);
            appointment.Ignore(a => a.OccupiesSlot);
        });

        modelBuilder.Entity<AppointmentHistoryEntry>(entry =>
        {
            entry.ToTable("appointment_history");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).ValueGeneratedNever();
            entry.HasIndex(e => new { e.AppointmentId, e.AtUtc });
            entry.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
            entry.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20);
            entry.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<AppointmentReminder>(reminder =>
        {
            reminder.ToTable("appointment_reminders");
            reminder.HasKey(r => r.Id);
            reminder.Property(r => r.Id).ValueGeneratedNever();
            reminder.Property(r => r.Kind).HasConversion<string>().HasMaxLength(20);

            // The whole idempotency guarantee, in one line. Not a code check:
            // a check followed by an insert has a gap, and two API instances
            // fit through it comfortably.
            reminder.HasIndex(r => new { r.AppointmentId, r.Kind }).IsUnique();
            reminder.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<AvailabilityRule>(rule =>
        {
            rule.ToTable("availability_rules");
            rule.HasKey(r => r.Id);
            rule.Property(r => r.Id).ValueGeneratedNever();
            rule.HasIndex(r => r.DayOfWeek);
            rule.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<AvailabilityOverride>(exception =>
        {
            exception.ToTable("availability_exceptions");
            exception.HasKey(e => e.Id);
            exception.Property(e => e.Id).ValueGeneratedNever();
            exception.HasIndex(e => e.DateLocal);
            exception.Property(e => e.Reason).HasMaxLength(200);
            exception.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<ServiceProjection>(service =>
        {
            service.ToTable("service_projections");
            // The key IS the Services module's id: the projection cannot
            // drift into having an identity of its own.
            service.HasKey(s => s.Id);
            service.Property(s => s.Id).ValueGeneratedNever();
            service.Property(s => s.Title).HasMaxLength(200);
            service.Property(s => s.Slug).HasMaxLength(200);
            service.HasIndex(s => s.Slug);
            service.Ignore(s => s.CanBeBooked);
            service.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<CustomerProjection>(customer =>
        {
            customer.ToTable("customer_projections");
            customer.HasKey(c => c.Id);
            customer.Property(c => c.Id).ValueGeneratedNever();
            customer.Property(c => c.Email).HasMaxLength(320);
            customer.Property(c => c.DisplayName).HasMaxLength(200);
            customer.Property(c => c.LastContactPhone).HasMaxLength(40);
            customer.Ignore(c => c.DomainEvents);
        });
    }
}

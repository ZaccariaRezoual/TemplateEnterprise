using EnterpriseFramework.Modules.Abstractions;
using EnterpriseFramework.Modules.Appointments.Features.Administration;
using EnterpriseFramework.Modules.Appointments.Features.Availability;
using EnterpriseFramework.Modules.Appointments.Features.Booking;
using EnterpriseFramework.Modules.Appointments.Features.Calendar;
using EnterpriseFramework.Modules.Appointments.Features.Queries;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using EnterpriseFramework.Modules.Authorization.Domain;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EnterpriseFramework.Modules.Appointments;

/// <summary>
/// Appointments module: public booking on real slots, an administrative
/// calendar, and the reminders around both.
///
/// Responsibilities: computing availability, taking requests, confirming and
/// moving bookings, and announcing every change as a public event so email
/// and realtime notifications happen without this module knowing either
/// exists.
///
/// Three design decisions shape everything here, and each is documented where
/// it lives:
/// - **A request reserves nothing.** Several people may ask for the same
///   hour; only a confirmation occupies it, and the database's exclusion
///   constraint is what makes double booking impossible rather than unlikely.
/// - **The business has one time zone.** Opening hours are local to it,
///   instants are UTC, and the conversion goes through
///   <see cref="TimeZoneInfo"/> — never through arithmetic on a
///   <see cref="DateTime"/>.
/// - **Availability is computed by a pure class**
///   (<c>AvailabilityCalculator</c>), so the cases that matter — the clock
///   change, the full day, the closure over an opening — can be tested
///   without a database.
///
/// The availability endpoint is ANONYMOUS: a visitor picks a time before
/// signing in. It answers with instants and nothing else, so it can say an
/// hour is taken without ever saying by whom.
/// </summary>
public sealed class AppointmentsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Appointments";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppointmentsOptions>(
            configuration.GetSection(AppointmentsOptions.SectionName)
        );

        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured."
            );

        services.AddDbContext<AppointmentsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        AppointmentsDbContext.Schema
                    )
            )
        );

        // The clock as a dependency, so "is this slot too soon?" and "may this
        // still be cancelled?" are testable without waiting for real time to
        // pass. TryAdd because another module may have registered it already.
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppointmentsModule).Assembly)
        );
        services.AddValidatorsFromAssembly(typeof(AppointmentsModule).Assembly);

        // Contributes the dashboard tiles. The Dashboard module never learns
        // this module exists; it resolves providers from the container.
        services.AddScoped<
            Application.Abstractions.IDashboardWidgetProvider,
            Features.Dashboard.AppointmentsWidgetProvider
        >();

        // Evaluates the two reminder rules every minute. The framework has no
        // scheduler by design, so the module brings its own — and the
        // idempotency comes from a unique index, not from this loop.
        services.AddHostedService<Features.Reminders.AppointmentReminderService>();

        if (configuration.ShouldAutoMigrate(Name))
        {
            services.AddHostedService<AppointmentsDbMigrator>();
        }
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapCustomerEndpoints(endpoints);
        MapAdminEndpoints(endpoints);
    }

    /// <summary>
    /// What a visitor and a customer reach: availability, booking, and their
    /// own appointments. No permission is involved — these are a person's own
    /// data, and a permission would be the wrong tool for "is this mine?".
    /// </summary>
    private static void MapCustomerEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/appointments").WithTags("Appointments");

        group
            .MapGet(
                "/availability",
                async (
                    Guid serviceId,
                    DateOnly from,
                    DateOnly to,
                    ISender sender,
                    CancellationToken ct
                ) => TypedResults.Ok(await sender.Send(new GetAvailabilityQuery(serviceId, from, to), ct))
            )
            .WithName("appointmentsAvailability")
            .WithSummary("Lists the bookable slots of a service, day by day.")
            // Anonymous by necessity: the visitor picks a time BEFORE being
            // asked to sign in, because asking first is what makes people
            // give up. Safe to expose because the answer is instants and
            // nothing else — it can say an hour is taken, never by whom.
            .AllowAnonymous()
            .ProducesValidationProblem();

        group
            .MapPost(
                "/",
                async (RequestAppointmentCommand command, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(command, ct))
            )
            .WithName("appointmentsRequest")
            .WithSummary("Asks for an appointment. It is a REQUEST: nothing is reserved yet.")
            .ProducesValidationProblem();

        group
            .MapGet(
                "/mine",
                async (ISender sender, CancellationToken ct, bool includePast = false) =>
                    TypedResults.Ok(await sender.Send(new ListMyAppointmentsQuery(includePast), ct))
            )
            .WithName("appointmentsMine")
            .WithSummary("Lists the caller's own appointments.");

        group
            .MapGet(
                "/{id:guid}/calendar.ics",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    var ics = await sender.Send(new GetAppointmentCalendarQuery(id), ct);

                    // A download, with a name: browsers hand an .ics to the
                    // calendar application by content type, and the file name
                    // is what the person sees if it lands in Downloads
                    // instead.
                    return Results.File(
                        System.Text.Encoding.UTF8.GetBytes(ics),
                        Contracts.Ics.IcsCalendar.ContentType,
                        "appointment.ics"
                    );
                }
            )
            .WithName("appointmentsCalendarFile")
            .WithSummary("Downloads one appointment as an iCalendar (.ics) file.");

        group
            .MapPost(
                "/{id:guid}/cancel",
                async (Guid id, CancelMyAppointmentRequest? body, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(
                        new CancelAppointmentCommand(
                            id,
                            string.IsNullOrWhiteSpace(body?.Reason)
                                ? "Cancelled by the customer."
                                : body.Reason,
                            ByCustomer: true
                        ),
                        ct
                    );
                    return TypedResults.NoContent();
                }
            )
            .WithName("appointmentsCancelMine")
            .WithSummary("Cancels one of the caller's own appointments.")
            .ProducesValidationProblem();
    }

    /// <summary>
    /// The calendar and everything that changes it, behind the appointments
    /// permissions.
    /// </summary>
    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin/appointments")
            .WithTags("Appointments administration");

        group
            .MapGet(
                "/",
                async (DateTime from, DateTime to, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new ListAppointmentsQuery(from, to), ct))
            )
            .WithName("adminAppointmentsList")
            .WithSummary("Lists the appointments overlapping a window of time.")
            .RequirePermission(Permissions.Appointments.Read)
            .ProducesValidationProblem();

        group
            .MapPost(
                "/{id:guid}/confirm",
                async (Guid id, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new ConfirmAppointmentCommand(id), ct))
            )
            .WithName("adminAppointmentsConfirm")
            .WithSummary("Confirms a request, cancelling the ones it displaces.")
            .RequirePermission(Permissions.Appointments.Write);

        group
            .MapPost(
                "/{id:guid}/reschedule",
                async (Guid id, RescheduleRequest body, ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(
                        await sender.Send(new RescheduleAppointmentCommand(id, body.StartUtc), ct)
                    )
            )
            .WithName("adminAppointmentsReschedule")
            .WithSummary("Moves an appointment to another time, revalidated server-side.")
            .RequirePermission(Permissions.Appointments.Write)
            .ProducesValidationProblem();

        group
            .MapPost(
                "/{id:guid}/cancel",
                async (Guid id, CancelRequest body, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(
                        new CancelAppointmentCommand(id, body.Reason, ByCustomer: false),
                        ct
                    );
                    return TypedResults.NoContent();
                }
            )
            .WithName("adminAppointmentsCancel")
            .WithSummary("Cancels an appointment on behalf of the business.")
            .RequirePermission(Permissions.Appointments.Write)
            .ProducesValidationProblem();

        group
            .MapGet(
                "/availability",
                async (ISender sender, CancellationToken ct) =>
                    TypedResults.Ok(await sender.Send(new GetAvailabilitySettingsQuery(), ct))
            )
            .WithName("adminAvailabilityGet")
            .WithSummary("Reads the weekly opening hours and their exceptions.")
            .RequirePermission(Permissions.Appointments.Read);

        group
            .MapPut(
                "/availability",
                async (
                    SaveAvailabilitySettingsCommand command,
                    ISender sender,
                    CancellationToken ct
                ) => TypedResults.Ok(await sender.Send(command, ct))
            )
            .WithName("adminAvailabilitySave")
            .WithSummary("Replaces the weekly opening hours and their exceptions.")
            .RequirePermission(Permissions.Appointments.Write)
            .ProducesValidationProblem();
    }
}

/// <summary>
/// Body of the customer's cancel endpoint. Optional: a customer explaining
/// themselves is a courtesy, not a requirement.
/// </summary>
/// <param name="Reason">Why they are calling it off.</param>
public sealed record CancelMyAppointmentRequest(string? Reason);

/// <summary>
/// Body of the administration's cancel endpoint.
/// </summary>
/// <param name="Reason">
/// Why. Required here, because the customer reads it: "cancelled" with no
/// explanation is the message that makes someone stop booking.
/// </param>
public sealed record CancelRequest(string Reason);

/// <summary>
/// Body of the reschedule endpoint.
/// </summary>
/// <param name="StartUtc">
/// New start instant, UTC. The duration is not settable: moving a booking
/// chooses WHEN, it does not redefine what was sold.
/// </param>
public sealed record RescheduleRequest(DateTime StartUtc);

/// <summary>
/// Applies this module's pending migrations at startup (development default;
/// production runs them as an explicit release step).
/// </summary>
public sealed class AppointmentsDbMigrator : IHostedService
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes the migrator.
    /// </summary>
    /// <param name="services">Root provider used to create a migration scope.</param>
    public AppointmentsDbMigrator(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Features.Booking;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Reminders;

/// <summary>
/// Sends the two reminders: one a fixed number of hours before the
/// appointment, one on the morning of the day itself.
///
/// The framework ships no scheduler — that is a deliberate omission, deferred
/// to the enterprise features — so this module brings the smallest one that
/// does the job: a loop that wakes every minute and asks "what is due?".
///
/// **Idempotency is not this loop's job.** Claiming a reminder means
/// inserting a row with a unique index on (appointment, kind); whoever
/// inserts it first publishes the event, and a second instance — two API
/// replicas during a rolling deploy, say — simply fails to insert and sends
/// nothing. A check-then-send in code would have a gap, and the gap is
/// exactly one duplicate email per replica.
///
/// The row is written BEFORE the event is published, on purpose. Crash in
/// between and a customer misses one reminder; the other way round, they get
/// one per crash.
///
/// Only CONFIRMED appointments earn reminders: telling someone about an
/// appointment nobody has accepted yet is worse than saying nothing.
/// </summary>
public sealed partial class AppointmentReminderService : BackgroundService
{
    /// <summary>
    /// How often the rules are evaluated. A minute is fine: both reminders
    /// are naturally fuzzy, and a tighter loop would only cost queries.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<AppointmentReminderService> _logger;

    [LoggerMessage(Level = LogLevel.Information, Message = "Appointment reminder sent ({Kind})")]
    private static partial void LogSent(ILogger logger, ReminderKind kind);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The appointment reminder sweep failed; it will run again next minute"
    )]
    private static partial void LogSweepFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <param name="services">Root provider used to create a scope per sweep.</param>
    /// <param name="logger">Logger used to report what was sent.</param>
    public AppointmentReminderService(
        IServiceProvider services,
        ILogger<AppointmentReminderService> logger
    )
    {
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // A failed sweep must not take the loop down with it: the
                // database being briefly unreachable would otherwise stop
                // every reminder until the next deploy.
                LogSweepFailed(_logger, exception);
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    /// <summary>Evaluates both rules once.</summary>
    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AppointmentsOptions>>().Value;
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var timeZone = options.ResolveTimeZone();

        await SendDueAsync(
                dbContext,
                eventBus,
                timeZone,
                ReminderKind.DayBefore,
                DayBeforeDue(nowUtc, options),
                cancellationToken
            )
            .ConfigureAwait(false);

        var sameDayWindow = SameDayDue(nowUtc, options, timeZone);
        if (sameDayWindow is not null)
        {
            await SendDueAsync(
                    dbContext,
                    eventBus,
                    timeZone,
                    ReminderKind.SameDayMorning,
                    sameDayWindow.Value,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The window of start times whose "N hours before" moment has arrived.
    /// </summary>
    private static (DateTime FromUtc, DateTime ToUtc) DayBeforeDue(
        DateTime nowUtc,
        AppointmentsOptions options
    ) => (nowUtc, nowUtc.AddHours(options.ReminderDayBeforeHours));

    /// <summary>
    /// The window of start times the morning reminder covers, or <c>null</c>
    /// when that moment has not arrived today.
    ///
    /// It is an hour of the DAY, not an offset, and that is the whole reason
    /// the two reminders are modelled differently: an appointment at 09:30
    /// and one at 18:00 must both be reminded at 08:00 local. Expressed as
    /// "N hours before" they would fire at 08:30 and 17:00, which is only
    /// correct for one appointment a day.
    /// </summary>
    private static (DateTime FromUtc, DateTime ToUtc)? SameDayDue(
        DateTime nowUtc,
        AppointmentsOptions options,
        TimeZoneInfo timeZone
    )
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var reminderTime = options.ResolveSameDayReminderTime();

        if (TimeOnly.FromDateTime(nowLocal) < reminderTime)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(nowLocal);
        var endOfDayLocal = today.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // From "now", not from the start of the day: an appointment that has
        // already happened does not need reminding about.
        return (nowUtc, TimeZoneInfo.ConvertTimeToUtc(endOfDayLocal, timeZone));
    }

    /// <summary>
    /// Claims and publishes every reminder of one kind whose moment has come.
    /// </summary>
    private async Task SendDueAsync(
        AppointmentsDbContext dbContext,
        IEventBus eventBus,
        TimeZoneInfo timeZone,
        ReminderKind kind,
        (DateTime FromUtc, DateTime ToUtc) window,
        CancellationToken cancellationToken
    )
    {
        var due = await dbContext
            .Appointments.AsNoTracking()
            .Where(appointment =>
                appointment.Status == AppointmentStatus.Confirmed
                && appointment.StartUtc > window.FromUtc
                && appointment.StartUtc <= window.ToUtc
                // The anti-join is only an optimisation: it keeps the sweep
                // from re-reading everything it already sent. The GUARANTEE
                // is the unique index below.
                && !dbContext.Reminders.Any(reminder =>
                    reminder.AppointmentId == appointment.Id && reminder.Kind == kind
                )
            )
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var appointment in due)
        {
            await ClaimAndPublishAsync(
                    dbContext,
                    eventBus,
                    timeZone,
                    kind,
                    appointment,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Inserts the claim and, only if it stuck, publishes the event.
    /// </summary>
    private async Task ClaimAndPublishAsync(
        AppointmentsDbContext dbContext,
        IEventBus eventBus,
        TimeZoneInfo timeZone,
        ReminderKind kind,
        Appointment appointment,
        CancellationToken cancellationToken
    )
    {
        dbContext.Reminders.Add(AppointmentReminder.Claim(appointment.Id, kind));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Someone else claimed it — another replica, or this sweep racing
            // the previous one. Nothing to do, and nothing to report: this is
            // the mechanism working, not failing.
            dbContext.ChangeTracker.Clear();
            return;
        }

        var service = await dbContext
            .Services.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == appointment.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        var customer = await dbContext
            .Customers.AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == appointment.CustomerUserId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (service is null)
        {
            // The claim stays: without the service there is nothing to name in
            // the message, and retrying every minute forever would be worse
            // than one missed reminder.
            return;
        }

        await eventBus
            .PublishAsync(
                new AppointmentReminderDue(
                    RequestAppointmentCommandHandler.Summarise(
                        appointment,
                        service,
                        customer,
                        timeZone
                    ),
                    kind
                ),
                cancellationToken
            )
            .ConfigureAwait(false);

        LogSent(_logger, kind);
    }
}

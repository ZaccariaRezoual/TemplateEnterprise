using System.Globalization;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using EnterpriseFramework.Modules.Authorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Dashboard;

/// <summary>
/// Contributes the appointment tiles to the dashboard.
///
/// It builds two very different tiles from the same provider, because which
/// one you get depends on who you are:
/// - whoever administers the calendar sees **today's appointments** and how
///   many requests are still waiting;
/// - everyone else sees **their own next appointment**, which is the only
///   thing about this module that concerns them.
///
/// The Dashboard module does not know this class exists — it resolves
/// providers from the container — and this class does not know the dashboard
/// renders tiles in a grid.
/// </summary>
public sealed class AppointmentsWidgetProvider : IDashboardWidgetProvider
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration, for the business time zone.</param>
    /// <param name="currentUser">Caller, for the permission check.</param>
    /// <param name="timeProvider">Clock.</param>
    public AppointmentsWidgetProvider(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options,
        ICurrentUser currentUser,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_currentUser.UserId is not { } userId)
        {
            return [];
        }

        var timeZone = _options.ResolveTimeZone();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        return _currentUser.Permissions.Contains(
            Permissions.Appointments.Read,
            StringComparer.Ordinal
        )
            ? await AdministrationTilesAsync(timeZone, nowUtc, cancellationToken).ConfigureAwait(false)
            : await CustomerTileAsync(userId, timeZone, nowUtc, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Today's confirmed appointments, plus the requests still waiting.</summary>
    private async Task<IReadOnlyList<DashboardWidget>> AdministrationTilesAsync(
        TimeZoneInfo timeZone,
        DateTime nowUtc,
        CancellationToken cancellationToken
    )
    {
        // "Today" is a local day, not 24 hours from now: an operator asking
        // what today looks like means the day on the wall calendar.
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(TimeOnly.MinValue), timeZone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(
            today.AddDays(1).ToDateTime(TimeOnly.MinValue),
            timeZone
        );

        var todayCount = await _dbContext
            .Appointments.AsNoTracking()
            .CountAsync(
                appointment =>
                    appointment.Status == AppointmentStatus.Confirmed
                    && appointment.StartUtc >= dayStartUtc
                    && appointment.StartUtc < dayEndUtc,
                cancellationToken
            )
            .ConfigureAwait(false);

        var pending = await _dbContext
            .Appointments.AsNoTracking()
            .CountAsync(
                appointment =>
                    appointment.Status == AppointmentStatus.Requested
                    && appointment.EndUtc >= nowUtc,
                cancellationToken
            )
            .ConfigureAwait(false);

        return
        [
            new DashboardWidget(
                "appointments.today",
                "Appuntamenti oggi",
                Value: todayCount.ToString(CultureInfo.InvariantCulture),
                // The number worth putting next to it: a request nobody
                // answered is somebody waiting, and it is invisible on a
                // calendar of confirmed bookings.
                Caption: $"{pending} richiesta/e da confermare",
                Link: "/appointments",
                Order: 5
            ),
        ];
    }

    /// <summary>The caller's own next appointment, if they have one.</summary>
    private async Task<IReadOnlyList<DashboardWidget>> CustomerTileAsync(
        Guid userId,
        TimeZoneInfo timeZone,
        DateTime nowUtc,
        CancellationToken cancellationToken
    )
    {
        var next = await _dbContext
            .Appointments.AsNoTracking()
            .Where(appointment =>
                appointment.CustomerUserId == userId
                && appointment.EndUtc >= nowUtc
                && (
                    appointment.Status == AppointmentStatus.Confirmed
                    || appointment.Status == AppointmentStatus.Requested
                )
            )
            .OrderBy(appointment => appointment.StartUtc)
            .Select(appointment => new
            {
                appointment.StartUtc,
                appointment.Status,
                appointment.ServiceId,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is null)
        {
            // No tile rather than an empty one: a dashboard full of "nothing
            // here" tiles is a dashboard nobody reads.
            return [];
        }

        var title = await _dbContext
            .Services.AsNoTracking()
            .Where(service => service.Id == next.ServiceId)
            .Select(service => service.Title)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(next.StartUtc, timeZone);

        return
        [
            new DashboardWidget(
                "appointments.next",
                "Prossimo appuntamento",
                Value: localStart.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture),
                Caption: next.Status == AppointmentStatus.Confirmed
                    ? title ?? "—"
                    // Said plainly, because it is the difference between a
                    // promise and a wait.
                    : $"{title ?? "—"} — in attesa di conferma",
                Link: "/my-appointments",
                Order: 5
            ),
        ];
    }
}

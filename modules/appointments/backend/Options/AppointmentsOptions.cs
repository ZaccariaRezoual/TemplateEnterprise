namespace EnterpriseFramework.Modules.Appointments.Options;

/// <summary>
/// Typed configuration of the Appointments module (section
/// "Modules:Appointments").
///
/// Every value here shapes the availability the public sees, so the defaults
/// are chosen to be sane for a small business rather than permissive: a
/// misconfigured deployment should offer too few slots, never too many.
/// </summary>
public sealed class AppointmentsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Modules:Appointments";

    /// <summary>
    /// Time zone the BUSINESS works in, as an IANA or Windows identifier.
    ///
    /// Availability is expressed in this zone: "we open at 9" means 9 there,
    /// not 9 UTC. Everything is PERSISTED in UTC and converted through
    /// <see cref="TimeZoneInfo"/>; the browser's zone is only ever used to
    /// display a time, never to compute one.
    /// </summary>
    public string TimeZone { get; init; } = "Europe/Rome";

    /// <summary>
    /// Step of the slot grid, in minutes. With 15, a 60-minute service is
    /// offered at 9:00, 9:15, 9:30 … rather than only on the hour.
    /// </summary>
    public int SlotGranularityMinutes { get; init; } = 15;

    /// <summary>
    /// Gap kept free before and after every appointment: cleaning, moving
    /// between rooms, breathing. Zero means back-to-back bookings.
    /// </summary>
    public int BufferMinutes { get; init; }

    /// <summary>
    /// How far ahead a slot must be to still be offered. Without it the site
    /// happily takes a booking for ten minutes from now.
    /// </summary>
    public int MinimumNoticeHours { get; init; } = 2;

    /// <summary>
    /// How far into the future the calendar goes. An unbounded horizon means
    /// someone books for two years' time and nobody remembers agreeing to it.
    /// </summary>
    public int MaxAdvanceDays { get; init; } = 60;

    /// <summary>Hours before the appointment for the first reminder.</summary>
    public int ReminderDayBeforeHours { get; init; } = 24;

    /// <summary>
    /// LOCAL time of day for the second reminder, "HH:mm".
    ///
    /// A time of day, not an offset, and that is the whole point: two
    /// appointments on the same day get the same morning reminder whatever
    /// their hour.
    /// </summary>
    public string ReminderSameDayLocalTime { get; init; } = "08:00";

    /// <summary>
    /// How close to the appointment a customer may still cancel it
    /// themselves. Past the cutoff they have to call, which is the point:
    /// a no-show an hour before costs the business the slot either way.
    /// </summary>
    public int CustomerCancellationCutoffHours { get; init; } = 24;

    /// <summary>
    /// Resolves <see cref="TimeZone"/> into a <see cref="TimeZoneInfo"/>.
    ///
    /// .NET accepts both IANA ("Europe/Rome") and Windows ("W. Europe
    /// Standard Time") identifiers on either platform, so a configuration
    /// written on Linux keeps working on Windows.
    /// </summary>
    /// <returns>The business time zone.</returns>
    /// <exception cref="TimeZoneNotFoundException">
    /// Thrown when the identifier is unknown. Deliberately not swallowed:
    /// falling back to UTC would silently shift every published opening hour.
    /// </exception>
    public TimeZoneInfo ResolveTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

    /// <summary>
    /// Parses <see cref="ReminderSameDayLocalTime"/>.
    /// </summary>
    /// <returns>The local time of day of the morning reminder.</returns>
    /// <exception cref="FormatException">Thrown when the value is not "HH:mm".</exception>
    public TimeOnly ResolveSameDayReminderTime() =>
        TimeOnly.ParseExact(
            ReminderSameDayLocalTime,
            "HH:mm",
            System.Globalization.CultureInfo.InvariantCulture
        );
}

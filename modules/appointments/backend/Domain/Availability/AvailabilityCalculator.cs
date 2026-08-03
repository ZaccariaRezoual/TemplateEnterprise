namespace EnterpriseFramework.Modules.Appointments.Domain.Availability;

/// <summary>
/// A span of time that is already taken.
/// </summary>
/// <param name="StartUtc">Start instant, UTC.</param>
/// <param name="EndUtc">End instant, UTC.</param>
public readonly record struct BusyInterval(DateTime StartUtc, DateTime EndUtc);

/// <summary>
/// One bookable slot.
/// </summary>
/// <param name="StartUtc">Start instant, UTC.</param>
/// <param name="EndUtc">End instant, UTC.</param>
public readonly record struct AvailableSlot(DateTime StartUtc, DateTime EndUtc);

/// <summary>
/// The slots of one local day.
/// </summary>
/// <param name="Date">The date, LOCAL to the business time zone.</param>
/// <param name="Slots">Bookable slots, in order. Empty means a closed or full day.</param>
public sealed record AvailableDay(DateOnly Date, IReadOnlyList<AvailableSlot> Slots);

/// <summary>
/// Everything the calculator needs that is not a rule, an exception or a
/// booking.
/// </summary>
/// <param name="TimeZone">
/// The BUSINESS time zone. Opening hours are local to it, and the whole grid
/// is built there before being converted.
/// </param>
/// <param name="ServiceDurationMinutes">How long the service takes.</param>
/// <param name="SlotGranularityMinutes">Step of the grid of candidate starts.</param>
/// <param name="BufferMinutes">Gap kept free before and after every booking.</param>
/// <param name="MinimumNoticeHours">How far ahead a slot must be to be offered.</param>
/// <param name="MaxAdvanceDays">How far into the future the calendar goes.</param>
/// <param name="NowUtc">The current instant, injected so the rules are testable.</param>
public sealed record AvailabilityRequest(
    TimeZoneInfo TimeZone,
    int ServiceDurationMinutes,
    int SlotGranularityMinutes,
    int BufferMinutes,
    int MinimumNoticeHours,
    int MaxAdvanceDays,
    DateTime NowUtc
);

/// <summary>
/// Works out which slots a service can actually be booked into.
///
/// **A pure class: no database, no HTTP, no clock.** Everything it needs is a
/// parameter, including the current instant. That is not tidiness — it is the
/// only way the cases that matter here can be tested at all, and the cases
/// that matter here are many: the day the clocks change, the day already
/// full, the closure that lands on an opening, the slot that is free but too
/// soon to book.
///
/// Responsibilities:
/// - Builds the grid of candidate starts, IN THE BUSINESS TIME ZONE.
/// - Removes what the weekly rules and the exceptions do not open.
/// - Removes what existing bookings occupy, plus the buffer around them.
/// - Removes what is too soon or too far ahead.
///
/// It does NOT decide whether a booking succeeds: between offering a slot and
/// confirming it there is a gap that only the database's exclusion constraint
/// can close.
/// </summary>
public static class AvailabilityCalculator
{
    /// <summary>
    /// Computes the bookable slots of every day in a local date range.
    /// </summary>
    /// <param name="from">First local date, inclusive.</param>
    /// <param name="to">Last local date, inclusive.</param>
    /// <param name="rules">Weekly openings.</param>
    /// <param name="exceptions">Closures and extraordinary openings.</param>
    /// <param name="busy">Spans already taken; only confirmed bookings belong here.</param>
    /// <param name="request">Time zone, durations and limits.</param>
    /// <returns>
    /// One entry per day of the range, in order, including the days with no
    /// slots — a calendar has to be able to grey out a closed day, and an
    /// absent day is indistinguishable from a day nobody asked about.
    /// </returns>
    public static IReadOnlyList<AvailableDay> Calculate(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<AvailabilityRule> rules,
        IReadOnlyCollection<AvailabilityOverride> exceptions,
        IReadOnlyCollection<BusyInterval> busy,
        AvailabilityRequest request
    )
    {
        var days = new List<AvailableDay>();

        if (to < from || request.ServiceDurationMinutes <= 0)
        {
            return days;
        }

        var duration = TimeSpan.FromMinutes(request.ServiceDurationMinutes);
        var step = TimeSpan.FromMinutes(Math.Max(1, request.SlotGranularityMinutes));
        var buffer = TimeSpan.FromMinutes(Math.Max(0, request.BufferMinutes));
        var earliestStartUtc = request.NowUtc.AddHours(request.MinimumNoticeHours);

        // The horizon is a LOCAL date, not an instant: "60 days ahead" means
        // 60 dates on the wall calendar, whatever the clocks did in between.
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(request.NowUtc, request.TimeZone)
        );
        var lastBookableDate = today.AddDays(Math.Max(0, request.MaxAdvanceDays));

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (date > lastBookableDate)
            {
                days.Add(new AvailableDay(date, []));
                continue;
            }

            var windows = WindowsFor(date, rules, exceptions);
            var slots = new List<AvailableSlot>();

            foreach (var (windowStart, windowEnd) in windows)
            {
                CollectSlots(date, windowStart, windowEnd, duration, step, buffer, busy, earliestStartUtc, request, slots);
            }

            // Two windows on the same day (a morning and an afternoon rule)
            // are computed independently, so the result needs ordering before
            // anyone renders it.
            slots.Sort((left, right) => left.StartUtc.CompareTo(right.StartUtc));
            days.Add(new AvailableDay(date, slots));
        }

        return days;
    }

    /// <summary>
    /// Works out which local windows are open on a date.
    ///
    /// An exception REPLACES the weekly rules rather than adding to them: a
    /// closure shuts the day whatever the rules say, and an extraordinary
    /// opening means those hours and no others. Merging the two would make
    /// "open 10–14 that Sunday" mean "and also the usual Sunday hours", which
    /// is never what anyone meant by writing it.
    /// </summary>
    private static List<(TimeOnly Start, TimeOnly End)> WindowsFor(
        DateOnly date,
        IReadOnlyCollection<AvailabilityRule> rules,
        IReadOnlyCollection<AvailabilityOverride> exceptions
    )
    {
        var forDate = exceptions.Where(exception => exception.DateLocal == date).ToList();

        if (forDate.Exists(exception => exception.IsClosed))
        {
            return [];
        }

        var extraordinary = forDate
            .Where(exception => exception.StartLocal is not null && exception.EndLocal is not null)
            .Select(exception => (Start: exception.StartLocal!.Value, End: exception.EndLocal!.Value))
            .Where(window => window.End > window.Start)
            .ToList();

        if (extraordinary.Count > 0)
        {
            return extraordinary;
        }

        return
        [
            .. rules
                .Where(rule => rule.DayOfWeek == date.DayOfWeek && rule.EndLocal > rule.StartLocal)
                .Select(rule => (Start: rule.StartLocal, End: rule.EndLocal)),
        ];
    }

    /// <summary>
    /// Walks one local window on the grid and keeps what is bookable.
    /// </summary>
    private static void CollectSlots(
        DateOnly date,
        TimeOnly windowStart,
        TimeOnly windowEnd,
        TimeSpan duration,
        TimeSpan step,
        TimeSpan buffer,
        IReadOnlyCollection<BusyInterval> busy,
        DateTime earliestStartUtc,
        AvailabilityRequest request,
        List<AvailableSlot> slots
    )
    {
        var windowStartLocal = date.ToDateTime(windowStart);
        var windowEndLocal = date.ToDateTime(windowEnd);

        // The walk happens on LOCAL wall-clock time, which is what makes the
        // clock-change days come out right on their own: on the spring
        // forward the missing hour has no valid instant and is skipped, and
        // on the autumn back the repeated hour is offered once. Adding the
        // step to a UTC instant instead would shift every opening by an hour
        // for half the year.
        for (
            var startLocal = windowStartLocal;
            startLocal + duration <= windowEndLocal;
            startLocal += step
        )
        {
            if (request.TimeZone.IsInvalidTime(startLocal))
            {
                // The hour the clocks skipped: it does not exist, so nobody
                // can turn up for it.
                continue;
            }

            var startUtc = ToUtc(startLocal, request.TimeZone);
            var endUtc = startUtc + duration;

            if (startUtc < earliestStartUtc)
            {
                continue;
            }

            if (Overlaps(startUtc, endUtc, buffer, busy))
            {
                continue;
            }

            slots.Add(new AvailableSlot(startUtc, endUtc));
        }
    }

    /// <summary>
    /// Converts a local wall-clock time to an instant.
    ///
    /// Ambiguous times — the hour that happens twice when the clocks go back —
    /// are resolved to the FIRST occurrence, deterministically. Either answer
    /// is defensible; picking one and documenting it is what stops the same
    /// input from producing two different calendars.
    /// </summary>
    private static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        if (!timeZone.IsAmbiguousTime(local))
        {
            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                timeZone
            );
        }

        // The larger offset is the daylight one, i.e. the earlier of the two
        // instants that share this wall-clock reading.
        var offsets = timeZone.GetAmbiguousTimeOffsets(local);
        var earliest = offsets.Max();

        return DateTime.SpecifyKind(local - earliest, DateTimeKind.Utc);
    }

    /// <summary>
    /// Tells whether a candidate slot collides with something already booked,
    /// once the buffer is taken into account.
    ///
    /// The buffer is applied to the CANDIDATE on both sides, which is
    /// equivalent to requiring a gap on either side of every existing
    /// booking — and cheaper than expanding every busy interval.
    /// </summary>
    private static bool Overlaps(
        DateTime startUtc,
        DateTime endUtc,
        TimeSpan buffer,
        IReadOnlyCollection<BusyInterval> busy
    )
    {
        var guardedStart = startUtc - buffer;
        var guardedEnd = endUtc + buffer;

        // Half-open intervals: a slot ending exactly when another starts does
        // NOT overlap, which is what "back to back" means with a zero buffer.
        return busy.Any(interval =>
            guardedStart < interval.EndUtc && interval.StartUtc < guardedEnd
        );
    }
}

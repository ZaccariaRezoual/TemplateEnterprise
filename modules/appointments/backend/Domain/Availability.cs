using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// A recurring weekly opening: "Tuesdays, 09:00 to 13:00".
///
/// The times are LOCAL to the business time zone, and stored as such. Storing
/// them in UTC would be wrong in the most confusing way possible: the opening
/// hour would move by an hour twice a year while the sign on the door did not.
/// </summary>
public sealed class AvailabilityRule : EntityBase<Guid>
{
    private AvailabilityRule() { }

    /// <summary>Day of the week this opening applies to.</summary>
    public DayOfWeek DayOfWeek { get; private set; }

    /// <summary>Local opening time.</summary>
    public TimeOnly StartLocal { get; private set; }

    /// <summary>Local closing time. Must be later than <see cref="StartLocal"/>.</summary>
    public TimeOnly EndLocal { get; private set; }

    /// <summary>
    /// Declares a weekly opening.
    /// </summary>
    /// <param name="dayOfWeek">Day it applies to.</param>
    /// <param name="startLocal">Local opening time.</param>
    /// <param name="endLocal">Local closing time.</param>
    /// <returns>The rule.</returns>
    public static AvailabilityRule Create(
        DayOfWeek dayOfWeek,
        TimeOnly startLocal,
        TimeOnly endLocal
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            DayOfWeek = dayOfWeek,
            StartLocal = startLocal,
            EndLocal = endLocal,
        };
}

/// <summary>
/// A one-off departure from the weekly schedule: a closure, or an
/// extraordinary opening.
///
/// One type for both, because they are the same question — "what applies on
/// this date?" — and two types would mean two lookups that can disagree.
/// </summary>
public sealed class AvailabilityOverride : EntityBase<Guid>
{
    private AvailabilityOverride() { }

    /// <summary>Local date the exception applies to.</summary>
    public DateOnly DateLocal { get; private set; }

    /// <summary>
    /// Whether the business is shut that day. When set, the times are ignored
    /// and no slot is offered whatever the weekly rules say.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>Local opening time of the extraordinary window.</summary>
    public TimeOnly? StartLocal { get; private set; }

    /// <summary>Local closing time of the extraordinary window.</summary>
    public TimeOnly? EndLocal { get; private set; }

    /// <summary>
    /// Why, in the words of whoever entered it. Shown in the administration
    /// only: a visitor sees a day with no slots, not "dentist appointment".
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Declares a full-day closure.
    /// </summary>
    /// <param name="dateLocal">Local date.</param>
    /// <param name="reason">Why, for the administration.</param>
    /// <returns>The exception.</returns>
    public static AvailabilityOverride Closure(DateOnly dateLocal, string reason) =>
        new()
        {
            Id = Guid.NewGuid(),
            DateLocal = dateLocal,
            IsClosed = true,
            Reason = reason.Trim(),
        };

    /// <summary>
    /// Declares an extraordinary opening, which REPLACES the weekly rules for
    /// that date rather than adding to them — "open 10 to 14 that Sunday"
    /// means those hours and no others.
    /// </summary>
    /// <param name="dateLocal">Local date.</param>
    /// <param name="startLocal">Local opening time.</param>
    /// <param name="endLocal">Local closing time.</param>
    /// <param name="reason">Why, for the administration.</param>
    /// <returns>The exception.</returns>
    public static AvailabilityOverride Opening(
        DateOnly dateLocal,
        TimeOnly startLocal,
        TimeOnly endLocal,
        string reason
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            DateLocal = dateLocal,
            IsClosed = false,
            StartLocal = startLocal,
            EndLocal = endLocal,
            Reason = reason.Trim(),
        };
}

namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// The state machine of an appointment, as a pure table.
///
/// It lives on its own — no entity, no database, no HTTP — because "may this
/// appointment be confirmed?" is a question with one right answer, asked from
/// several places: the command handlers, the calendar that decides which
/// buttons to render, and the tests. A rule reimplemented per caller is a rule
/// that will disagree with itself.
///
/// Two moves are deliberately absent and worth naming:
/// - **Nothing leaves <see cref="AppointmentStatus.Cancelled"/>.** Reviving a
///   cancelled appointment would resurrect a slot the customer was already
///   told they had lost.
/// - **A request cannot be completed or marked as a no-show.** Nobody can fail
///   to turn up to an appointment that was never accepted.
/// </summary>
public static class AppointmentTransitions
{
    private static readonly Dictionary<
        AppointmentStatus,
        IReadOnlySet<AppointmentStatus>
    > Allowed = new()
    {
        [AppointmentStatus.Requested] = new HashSet<AppointmentStatus>
        {
            AppointmentStatus.Confirmed,
            AppointmentStatus.Cancelled,
        },
        [AppointmentStatus.Confirmed] = new HashSet<AppointmentStatus>
        {
            AppointmentStatus.Completed,
            AppointmentStatus.Cancelled,
            AppointmentStatus.NoShow,
        },
        [AppointmentStatus.Completed] = new HashSet<AppointmentStatus>(),
        [AppointmentStatus.Cancelled] = new HashSet<AppointmentStatus>(),
        [AppointmentStatus.NoShow] = new HashSet<AppointmentStatus>(),
    };

    /// <summary>
    /// Tells whether a status may move to another.
    /// </summary>
    /// <param name="from">Current status.</param>
    /// <param name="to">Status being asked for.</param>
    /// <returns><c>true</c> when the move is legal.</returns>
    public static bool IsAllowed(AppointmentStatus from, AppointmentStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>
    /// Tells whether an appointment in this status still occupies its slot.
    ///
    /// Only <see cref="AppointmentStatus.Confirmed"/> does, which is the same
    /// rule the database's exclusion constraint enforces — stated here so the
    /// availability engine and the constraint cannot drift apart.
    /// </summary>
    /// <param name="status">Status to test.</param>
    /// <returns><c>true</c> when the slot is taken.</returns>
    public static bool OccupiesSlot(AppointmentStatus status) =>
        status == AppointmentStatus.Confirmed;

    /// <summary>
    /// Tells whether an appointment in this status can still change.
    /// </summary>
    /// <param name="status">Status to test.</param>
    /// <returns><c>true</c> when at least one move is still legal.</returns>
    public static bool IsOpen(AppointmentStatus status) =>
        Allowed.TryGetValue(status, out var targets) && targets.Count > 0;
}

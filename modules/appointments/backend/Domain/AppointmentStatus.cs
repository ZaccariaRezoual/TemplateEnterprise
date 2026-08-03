namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// Where an appointment is in its life.
///
/// Stored and serialized as its NAME, never as an ordinal: inserting a value
/// in the middle would otherwise silently change the meaning of every row
/// already written and every integer in flight.
///
/// The legal moves are:
/// <code>
/// Requested → Confirmed → Completed
///     ↓           ↓
/// Cancelled   Cancelled / NoShow
/// </code>
/// Anything else is refused by <see cref="Appointment"/>, not by a comment.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>
    /// The customer asked. Nothing is reserved yet — the exclusion constraint
    /// deliberately ignores this state, so several people may ask for the
    /// same hour and whoever administers chooses.
    /// </summary>
    Requested = 0,

    /// <summary>
    /// Accepted. This is the only state that OCCUPIES the slot, and the only
    /// one that earns reminders.
    /// </summary>
    Confirmed = 1,

    /// <summary>It happened.</summary>
    Completed = 2,

    /// <summary>Called off, by either side. Terminal.</summary>
    Cancelled = 3,

    /// <summary>
    /// Confirmed, and nobody came. Terminal, and kept distinct from
    /// <see cref="Cancelled"/> because "they cancelled" and "they did not turn
    /// up" are different facts about a customer.
    /// </summary>
    NoShow = 4,
}

using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Appointments.Contracts.Events;

/// <summary>
/// Everything a subscriber needs to talk to a human about an appointment,
/// carried by every event of this module.
///
/// It travels IN the events rather than being looked up because a subscriber
/// must not query this module's tables — that is the rule that keeps modules
/// separately removable. The Email module composes a message from this and
/// nothing else.
///
/// PUBLIC CONTRACT: add fields, never remove or repurpose them.
/// </summary>
/// <param name="AppointmentId">Identifier of the appointment.</param>
/// <param name="CustomerUserId">Account that booked it.</param>
/// <param name="CustomerEmail">Where to write to the customer.</param>
/// <param name="CustomerName">How to address them.</param>
/// <param name="ServiceId">Service that was booked, referenced by id.</param>
/// <param name="ServiceTitle">
/// Title of the service AS IT WAS when the event was raised. A copy, not a
/// lookup: a message about an appointment must not change meaning because
/// someone renamed the service afterwards.
/// </param>
/// <param name="StartUtc">Start instant, always UTC.</param>
/// <param name="EndUtc">End instant, always UTC.</param>
/// <param name="TimeZoneId">
/// IANA identifier of the business time zone, so a subscriber can render a
/// local time without knowing this module's configuration.
/// </param>
public sealed record AppointmentSummary(
    Guid AppointmentId,
    Guid CustomerUserId,
    string CustomerEmail,
    string CustomerName,
    Guid ServiceId,
    string ServiceTitle,
    DateTime StartUtc,
    DateTime EndUtc,
    string TimeZoneId
);

/// <summary>
/// Published when a customer asks for an appointment.
///
/// PUBLIC CONTRACT. Known subscribers: Email (writes to both sides),
/// Notifications (via the module's own bridge).
///
/// A request is NOT a booking: nothing is reserved until an administrator
/// confirms it, and every message produced from this event has to say so.
/// </summary>
/// <param name="Appointment">Who, what and when.</param>
/// <param name="CustomerNote">Optional note the customer wrote.</param>
public sealed record AppointmentRequested(AppointmentSummary Appointment, string? CustomerNote)
    : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

/// <summary>
/// Published when an administrator accepts a request.
///
/// PUBLIC CONTRACT. This is the first event whose message may say "your
/// appointment", and the one that carries the calendar attachment.
/// </summary>
/// <param name="Appointment">Who, what and when.</param>
public sealed record AppointmentConfirmed(AppointmentSummary Appointment) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

/// <summary>
/// Published when a confirmed appointment moves to another time.
///
/// PUBLIC CONTRACT. <paramref name="Appointment"/> already carries the NEW
/// time; the previous one travels separately because a message that does not
/// say what it was leaves the reader checking their own calendar.
/// </summary>
/// <param name="Appointment">Who, what and when — after the move.</param>
/// <param name="PreviousStartUtc">Where it was before.</param>
/// <param name="MovedByCustomer">
/// Whether the customer moved it themselves, which decides who needs telling.
/// </param>
public sealed record AppointmentRescheduled(
    AppointmentSummary Appointment,
    DateTime PreviousStartUtc,
    bool MovedByCustomer
) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

/// <summary>
/// Published when an appointment or a request is called off.
///
/// PUBLIC CONTRACT. It covers three situations that read very differently to
/// the person receiving the message — the customer cancelled, the business
/// cancelled, or the slot went to someone else — so the reason and the actor
/// both travel with it.
/// </summary>
/// <param name="Appointment">Who, what and when.</param>
/// <param name="Reason">Client-safe explanation; may be shown to the customer.</param>
/// <param name="CancelledByCustomer">
/// Whether the customer called it off, which decides who needs telling.
/// </param>
public sealed record AppointmentCancelled(
    AppointmentSummary Appointment,
    string Reason,
    bool CancelledByCustomer
) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

/// <summary>Which of the two reminders is due.</summary>
public enum ReminderKind
{
    /// <summary>Sent 24 hours before the appointment (configurable).</summary>
    DayBefore = 0,

    /// <summary>
    /// Sent on the morning of the appointment, at a fixed LOCAL time.
    ///
    /// Deliberately not modelled as an offset: an appointment at 09:30 and one
    /// at 18:00 must both be reminded at 08:00, not at 08:30 and 17:00.
    /// </summary>
    SameDayMorning = 1,
}

/// <summary>
/// Published when a reminder for a confirmed appointment comes due.
///
/// PUBLIC CONTRACT. It is raised at most ONCE per appointment and kind, and
/// the guarantee comes from a unique constraint in the database rather than
/// from the scheduler — two API instances running the same minute would
/// otherwise both send it.
/// </summary>
/// <param name="Appointment">Who, what and when.</param>
/// <param name="Kind">Which reminder came due.</param>
public sealed record AppointmentReminderDue(AppointmentSummary Appointment, ReminderKind Kind)
    : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

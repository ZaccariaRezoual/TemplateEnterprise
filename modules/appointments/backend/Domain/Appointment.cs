using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// One booking: a customer, a service, and a span of time.
///
/// Responsibilities:
/// - Holds the booking and its contact details.
/// - Enforces the state machine (<see cref="AppointmentTransitions"/>): every
///   move returns whether it was legal instead of silently doing nothing.
/// - Records its own history, because "you moved it, not me" is a
///   conversation that only a log ends.
///
/// It contains no persistence logic and no notification logic: the handlers
/// publish the public events, because those carry the customer's email and
/// the service title, which live in projections rather than here.
///
/// **Times are UTC. Always.** The business time zone shapes which slots are
/// OFFERED (see <see cref="Availability.AvailabilityCalculator"/>); what is
/// stored is an instant, and an instant has no time zone.
/// </summary>
public sealed class Appointment : EntityBase<Guid>
{
    private readonly List<AppointmentHistoryEntry> _history = [];

    private Appointment() { }

    /// <summary>
    /// Service that was booked, referenced BY ID.
    ///
    /// Never a foreign key: the service lives in another module's schema, and
    /// the two must stay separately installable. What this module needs to
    /// display is kept in its own <see cref="ServiceProjection"/>.
    /// </summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Account that booked it.</summary>
    public Guid CustomerUserId { get; private set; }

    /// <summary>Start instant, UTC.</summary>
    public DateTime StartUtc { get; private set; }

    /// <summary>End instant, UTC. Start plus the service duration.</summary>
    public DateTime EndUtc { get; private set; }

    /// <summary>Where the appointment is in its life.</summary>
    public AppointmentStatus Status { get; private set; }

    /// <summary>
    /// Phone number for THIS booking, always present.
    ///
    /// On the appointment rather than only on the profile because it is the
    /// contact for this booking: someone booking on behalf of a relative
    /// legitimately leaves a different number. It is required because a
    /// missed appointment is recovered with a call, not with an unread email.
    /// </summary>
    public string ContactPhone { get; private set; } = string.Empty;

    /// <summary>Optional note the customer wrote when booking.</summary>
    public string? CustomerNote { get; private set; }

    /// <summary>
    /// Internal note. NEVER returned by a customer-facing endpoint — the
    /// customer DTO simply does not have the field, so it cannot leak by
    /// someone forgetting to strip it.
    /// </summary>
    public string? AdminNote { get; private set; }

    /// <summary>
    /// Why it was called off, when it was. Shown to the customer, so it is
    /// written for them: "the slot is no longer available", not a code.
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>Everything that happened to this appointment, oldest first.</summary>
    public IReadOnlyCollection<AppointmentHistoryEntry> History => _history.AsReadOnly();

    /// <summary>UTC instant the booking was made.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>UTC instant of the last change.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Whether this appointment currently occupies its slot.</summary>
    public bool OccupiesSlot => AppointmentTransitions.OccupiesSlot(Status);

    /// <summary>
    /// Records a customer's request.
    ///
    /// It starts as <see cref="AppointmentStatus.Requested"/> and reserves
    /// nothing: until an administrator confirms, that hour belongs to nobody,
    /// and every message about it has to say so.
    /// </summary>
    /// <param name="serviceId">Service being booked.</param>
    /// <param name="customerUserId">Account making the request.</param>
    /// <param name="startUtc">Start instant, UTC.</param>
    /// <param name="endUtc">End instant, UTC.</param>
    /// <param name="contactPhone">Phone number for this booking.</param>
    /// <param name="customerNote">Optional note from the customer.</param>
    /// <returns>The new request.</returns>
    public static Appointment Request(
        Guid serviceId,
        Guid customerUserId,
        DateTime startUtc,
        DateTime endUtc,
        string contactPhone,
        string? customerNote
    )
    {
        var now = DateTime.UtcNow;
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            CustomerUserId = customerUserId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = AppointmentStatus.Requested,
            ContactPhone = contactPhone.Trim(),
            CustomerNote = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        appointment._history.Add(
            AppointmentHistoryEntry.Record(
                appointment.Id,
                customerUserId,
                fromStatus: null,
                toStatus: AppointmentStatus.Requested,
                fromStartUtc: null,
                toStartUtc: startUtc
            )
        );

        return appointment;
    }

    /// <summary>
    /// Accepts the request.
    /// </summary>
    /// <param name="byUserId">Administrator confirming it.</param>
    /// <returns><c>false</c> when the current status forbids it.</returns>
    public bool Confirm(Guid byUserId) => MoveTo(AppointmentStatus.Confirmed, byUserId);

    /// <summary>
    /// Calls the appointment off.
    /// </summary>
    /// <param name="reason">Client-safe explanation, shown to the customer.</param>
    /// <param name="byUserId">Who called it off.</param>
    /// <returns><c>false</c> when the current status forbids it.</returns>
    public bool Cancel(string reason, Guid byUserId)
    {
        if (!MoveTo(AppointmentStatus.Cancelled, byUserId))
        {
            return false;
        }

        CancellationReason = reason.Trim();
        return true;
    }

    /// <summary>Marks a confirmed appointment as having taken place.</summary>
    /// <param name="byUserId">Who recorded it.</param>
    /// <returns><c>false</c> when the current status forbids it.</returns>
    public bool Complete(Guid byUserId) => MoveTo(AppointmentStatus.Completed, byUserId);

    /// <summary>Records that the customer did not turn up.</summary>
    /// <param name="byUserId">Who recorded it.</param>
    /// <returns><c>false</c> when the current status forbids it.</returns>
    public bool MarkNoShow(Guid byUserId) => MoveTo(AppointmentStatus.NoShow, byUserId);

    /// <summary>
    /// Moves the appointment to another time, keeping its status.
    ///
    /// Refused once the appointment is closed: moving a cancelled booking
    /// would put back on the calendar something both sides consider over.
    /// Whether the NEW time is actually free is not decided here — the
    /// database's exclusion constraint is the only place that can answer that
    /// without a race.
    /// </summary>
    /// <param name="startUtc">New start instant, UTC.</param>
    /// <param name="endUtc">New end instant, UTC.</param>
    /// <param name="byUserId">Who moved it.</param>
    /// <returns><c>false</c> when the appointment is already closed.</returns>
    public bool Reschedule(DateTime startUtc, DateTime endUtc, Guid byUserId)
    {
        if (!AppointmentTransitions.IsOpen(Status))
        {
            return false;
        }

        var previousStart = StartUtc;
        StartUtc = startUtc;
        EndUtc = endUtc;
        UpdatedAtUtc = DateTime.UtcNow;

        _history.Add(
            AppointmentHistoryEntry.Record(Id, byUserId, Status, Status, previousStart, startUtc)
        );

        return true;
    }

    /// <summary>
    /// Sets the internal note, which the customer never sees.
    /// </summary>
    /// <param name="note">The note, or null to clear it.</param>
    public void SetAdminNote(string? note)
    {
        AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Applies a status change and records it, if the move is legal.</summary>
    private bool MoveTo(AppointmentStatus target, Guid byUserId)
    {
        if (!AppointmentTransitions.IsAllowed(Status, target))
        {
            return false;
        }

        var previous = Status;
        Status = target;
        UpdatedAtUtc = DateTime.UtcNow;

        _history.Add(
            AppointmentHistoryEntry.Record(Id, byUserId, previous, target, StartUtc, StartUtc)
        );

        return true;
    }
}

using EnterpriseFramework.Modules.Appointments.Domain;

namespace EnterpriseFramework.Modules.Appointments.Contracts;

/// <summary>
/// One bookable slot, as offered to a client.
/// </summary>
/// <param name="StartUtc">Start instant, UTC. The browser renders it locally.</param>
/// <param name="EndUtc">End instant, UTC.</param>
public sealed record SlotDto(DateTime StartUtc, DateTime EndUtc);

/// <summary>
/// The slots of one day.
/// </summary>
/// <param name="Date">The date, LOCAL to the business time zone, as "yyyy-MM-dd".</param>
/// <param name="Slots">
/// Bookable slots, in order. An EMPTY list is meaningful and is why closed
/// days are returned at all: a calendar has to grey out a Sunday, and a
/// missing day is indistinguishable from a day nobody asked about.
/// </param>
public sealed record AvailableDayDto(string Date, IReadOnlyList<SlotDto> Slots);

/// <summary>
/// The answer to "when can I book this service?".
/// </summary>
/// <param name="ServiceId">Service the slots are for.</param>
/// <param name="ServiceTitle">Its title, so the page can name it in one call.</param>
/// <param name="DurationMinutes">How long each slot lasts.</param>
/// <param name="TimeZoneId">
/// The business time zone. The client needs it to say "09:00 our time" rather
/// than converting into the visitor's own zone and quietly booking them for
/// the wrong hour in their head.
/// </param>
/// <param name="Days">One entry per day of the requested range.</param>
public sealed record AvailabilityDto(
    Guid ServiceId,
    string ServiceTitle,
    int DurationMinutes,
    string TimeZoneId,
    IReadOnlyList<AvailableDayDto> Days
);

/// <summary>
/// An appointment as its OWNER sees it.
///
/// It deliberately has no <c>AdminNote</c> field. Not "we remember to strip
/// it" — the shape simply cannot carry it, so it cannot leak by omission.
/// </summary>
/// <param name="Id">Identifier of the appointment.</param>
/// <param name="ServiceId">Service that was booked.</param>
/// <param name="ServiceTitle">Its title, from this module's projection.</param>
/// <param name="StartUtc">Start instant, UTC.</param>
/// <param name="EndUtc">End instant, UTC.</param>
/// <param name="Status">Where it is in its life.</param>
/// <param name="ContactPhone">Number left for this booking.</param>
/// <param name="CustomerNote">Note the customer wrote, if any.</param>
/// <param name="CancellationReason">Why it was called off, if it was.</param>
/// <param name="CanCancel">
/// Whether the customer may still call it off themselves. Computed by the
/// server: the cutoff is configuration, and the client must not have to know
/// it — the API stays the boundary that enforces it either way.
/// </param>
/// <param name="CreatedAtUtc">When it was booked.</param>
public sealed record MyAppointmentDto(
    Guid Id,
    Guid ServiceId,
    string ServiceTitle,
    DateTime StartUtc,
    DateTime EndUtc,
    AppointmentStatus Status,
    string ContactPhone,
    string? CustomerNote,
    string? CancellationReason,
    bool CanCancel,
    DateTime CreatedAtUtc
);

/// <summary>
/// An appointment as the ADMINISTRATION sees it: everything, including who
/// booked it and the internal note.
/// </summary>
/// <param name="Id">Identifier of the appointment.</param>
/// <param name="ServiceId">Service that was booked.</param>
/// <param name="ServiceTitle">Its title, from this module's projection.</param>
/// <param name="CustomerUserId">Account that booked it.</param>
/// <param name="CustomerName">Name of the customer.</param>
/// <param name="CustomerEmail">Email of the customer.</param>
/// <param name="StartUtc">Start instant, UTC.</param>
/// <param name="EndUtc">End instant, UTC.</param>
/// <param name="Status">Where it is in its life.</param>
/// <param name="ContactPhone">Number left for this booking.</param>
/// <param name="CustomerNote">Note the customer wrote, if any.</param>
/// <param name="AdminNote">Internal note; never shown to the customer.</param>
/// <param name="CancellationReason">Why it was called off, if it was.</param>
/// <param name="HasConflict">
/// Whether this REQUEST overlaps an already-confirmed appointment.
///
/// Computed for the calendar so a doomed request is shown as such: confirming
/// it would be refused by the database, and finding that out by clicking is a
/// poor way to learn it.
/// </param>
/// <param name="CreatedAtUtc">When it was booked.</param>
/// <param name="History">What happened to it, oldest first.</param>
public sealed record AdminAppointmentDto(
    Guid Id,
    Guid ServiceId,
    string ServiceTitle,
    Guid CustomerUserId,
    string CustomerName,
    string CustomerEmail,
    DateTime StartUtc,
    DateTime EndUtc,
    AppointmentStatus Status,
    string ContactPhone,
    string? CustomerNote,
    string? AdminNote,
    string? CancellationReason,
    bool HasConflict,
    DateTime CreatedAtUtc,
    IReadOnlyList<AppointmentHistoryDto> History
);

/// <summary>
/// One line of an appointment's history.
/// </summary>
/// <param name="AtUtc">When it happened.</param>
/// <param name="ChangedByUserId">Who did it.</param>
/// <param name="FromStatus">Status before, or null for the booking itself.</param>
/// <param name="ToStatus">Status after.</param>
/// <param name="FromStartUtc">Start before, or null for the booking itself.</param>
/// <param name="ToStartUtc">Start after.</param>
public sealed record AppointmentHistoryDto(
    DateTime AtUtc,
    Guid ChangedByUserId,
    AppointmentStatus? FromStatus,
    AppointmentStatus ToStatus,
    DateTime? FromStartUtc,
    DateTime ToStartUtc
)
{
    /// <summary>
    /// Maps a history entry to its client representation.
    /// </summary>
    /// <param name="entry">The entry to map.</param>
    /// <returns>The DTO.</returns>
    public static AppointmentHistoryDto FromEntry(AppointmentHistoryEntry entry) =>
        new(
            entry.AtUtc,
            entry.ChangedByUserId,
            entry.FromStatus,
            entry.ToStatus,
            entry.FromStartUtc,
            entry.ToStartUtc
        );
}

/// <summary>
/// One weekly opening, as exposed to the administration.
/// </summary>
/// <param name="DayOfWeek">Day it applies to.</param>
/// <param name="StartLocal">Local opening time, "HH:mm".</param>
/// <param name="EndLocal">Local closing time, "HH:mm".</param>
public sealed record AvailabilityRuleDto(DayOfWeek DayOfWeek, string StartLocal, string EndLocal);

/// <summary>
/// One closure or extraordinary opening, as exposed to the administration.
/// </summary>
/// <param name="DateLocal">Local date, "yyyy-MM-dd".</param>
/// <param name="IsClosed">Whether the business is shut that day.</param>
/// <param name="StartLocal">Local opening time of the extraordinary window, "HH:mm".</param>
/// <param name="EndLocal">Local closing time of the extraordinary window, "HH:mm".</param>
/// <param name="Reason">Why. Administration only; a visitor never sees it.</param>
public sealed record AvailabilityOverrideDto(
    string DateLocal,
    bool IsClosed,
    string? StartLocal,
    string? EndLocal,
    string Reason
);

/// <summary>
/// The whole availability configuration, read and written as one document.
///
/// One shape rather than per-row endpoints because that is how it is edited:
/// a weekly schedule is thought about, and saved, as a whole. Saving one rule
/// at a time turns "move Tuesday to the afternoon" into three requests that
/// can half-fail.
/// </summary>
/// <param name="TimeZoneId">The business time zone these times are local to.</param>
/// <param name="Rules">Recurring weekly openings.</param>
/// <param name="Exceptions">Closures and extraordinary openings.</param>
public sealed record AvailabilitySettingsDto(
    string TimeZoneId,
    IReadOnlyList<AvailabilityRuleDto> Rules,
    IReadOnlyList<AvailabilityOverrideDto> Exceptions
);

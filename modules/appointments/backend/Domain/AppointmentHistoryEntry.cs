using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// One thing that happened to an appointment: who did it, when, and what
/// changed.
///
/// The Audit module already records the commands that ran, and this is not a
/// duplicate of it. Audit answers "what did this system do?" for an operator
/// reading a log; this answers "what happened to MY appointment?" for the two
/// people arguing about it, and it has to be readable inside the appointment
/// itself.
/// </summary>
public sealed class AppointmentHistoryEntry : EntityBase<Guid>
{
    private AppointmentHistoryEntry() { }

    /// <summary>Appointment this entry belongs to.</summary>
    public Guid AppointmentId { get; private set; }

    /// <summary>Account that made the change.</summary>
    public Guid ChangedByUserId { get; private set; }

    /// <summary>Status before, or <c>null</c> for the entry that created it.</summary>
    public AppointmentStatus? FromStatus { get; private set; }

    /// <summary>Status after.</summary>
    public AppointmentStatus ToStatus { get; private set; }

    /// <summary>Start instant before, or <c>null</c> for the creating entry.</summary>
    public DateTime? FromStartUtc { get; private set; }

    /// <summary>Start instant after. Equal to the previous one unless it moved.</summary>
    public DateTime ToStartUtc { get; private set; }

    /// <summary>UTC instant of the change.</summary>
    public DateTime AtUtc { get; private set; }

    /// <summary>
    /// Records an entry.
    /// </summary>
    /// <param name="appointmentId">Appointment the entry belongs to.</param>
    /// <param name="changedByUserId">Account that made the change.</param>
    /// <param name="fromStatus">Status before, or null when it was created.</param>
    /// <param name="toStatus">Status after.</param>
    /// <param name="fromStartUtc">Start before, or null when it was created.</param>
    /// <param name="toStartUtc">Start after.</param>
    /// <returns>The entry.</returns>
    internal static AppointmentHistoryEntry Record(
        Guid appointmentId,
        Guid changedByUserId,
        AppointmentStatus? fromStatus,
        AppointmentStatus toStatus,
        DateTime? fromStartUtc,
        DateTime toStartUtc
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            ChangedByUserId = changedByUserId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            FromStartUtc = fromStartUtc,
            ToStartUtc = toStartUtc,
            AtUtc = DateTime.UtcNow,
        };
}

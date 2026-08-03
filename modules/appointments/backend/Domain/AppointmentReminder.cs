using EnterpriseFramework.Domain.Common;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;

namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// The record that a reminder has been sent.
///
/// **Its only job is to make sending idempotent, and the database does the
/// work**: a unique index on (appointment, kind) means the second attempt
/// fails to insert and therefore sends nothing. Two API instances evaluating
/// the same minute is not a hypothetical — it is what a rolling deploy looks
/// like — and the alternative is the customer getting the same email twice,
/// which is the fastest way to make them switch notifications off.
///
/// The row is written BEFORE the event is published, on purpose. If the
/// process dies in between, the customer misses one reminder; the other way
/// round, they get one per crash.
/// </summary>
public sealed class AppointmentReminder : EntityBase<Guid>
{
    private AppointmentReminder() { }

    /// <summary>Appointment the reminder is about.</summary>
    public Guid AppointmentId { get; private set; }

    /// <summary>Which of the two reminders this is.</summary>
    public ReminderKind Kind { get; private set; }

    /// <summary>UTC instant the reminder was claimed.</summary>
    public DateTime SentAtUtc { get; private set; }

    /// <summary>
    /// Claims a reminder. Inserting this row is what wins the race.
    /// </summary>
    /// <param name="appointmentId">Appointment the reminder is about.</param>
    /// <param name="kind">Which reminder.</param>
    /// <returns>The row to insert.</returns>
    public static AppointmentReminder Claim(Guid appointmentId, ReminderKind kind) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Kind = kind,
            SentAtUtc = DateTime.UtcNow,
        };
}

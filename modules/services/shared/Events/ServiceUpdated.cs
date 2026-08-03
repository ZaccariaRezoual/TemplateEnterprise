using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Services.Contracts.Events;

/// <summary>
/// Published when a service that the public already knows about changes.
///
/// PUBLIC CONTRACT: other modules subscribe to this type, so changing it is a
/// breaking change for them. Add fields, never remove or repurpose them.
/// Known subscribers today: none. Planned: Appointments, which refreshes its
/// projection so a calendar entry taken last month still shows the current
/// name of the service.
///
/// It is raised only for a service that WAS published before the change —
/// including the change that unpublishes it, which is exactly the case a
/// subscriber must hear about: withdrawing a service has to stop new
/// bookings, and there is no separate "unpublished" event to forget to
/// handle.
/// </summary>
/// <param name="ServiceId">Identifier of the service.</param>
/// <param name="Title">Title after the change.</param>
/// <param name="Slug">
/// URL segment after the change. It can differ from the previous one: the
/// administration warns before letting it happen, but does not forbid it.
/// </param>
/// <param name="DurationMinutes">
/// Duration of one appointment after the change, or <c>null</c> when the
/// service is only described.
/// </param>
/// <param name="IsBookable">
/// Whether the service accepts new bookings RIGHT NOW — the effective value,
/// not just the flag: an unpublished or archived service is never bookable.
/// A subscriber can therefore honour it without knowing this module's
/// publication rules.
/// </param>
public sealed record ServiceUpdated(
    Guid ServiceId,
    string Title,
    string Slug,
    int? DurationMinutes,
    bool IsBookable
) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

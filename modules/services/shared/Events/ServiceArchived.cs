using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Services.Contracts.Events;

/// <summary>
/// Published when a service is withdrawn from the catalogue.
///
/// PUBLIC CONTRACT: other modules subscribe to this type, so changing it is a
/// breaking change for them. Add fields, never remove or repurpose them.
/// Known subscribers today: none. Planned: Appointments, for which this event
/// is what stops NEW bookings while leaving the ones already taken readable.
///
/// Archiving is not deletion, and the difference is deliberate: another
/// module may hold references to this identifier, and this module cannot know
/// it — that is the same reason there are no foreign keys across schemas. The
/// identifier is never reused.
/// </summary>
/// <param name="ServiceId">Identifier of the archived service.</param>
public sealed record ServiceArchived(Guid ServiceId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

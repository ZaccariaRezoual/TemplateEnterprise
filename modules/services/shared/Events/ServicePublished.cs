using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Services.Contracts.Events;

/// <summary>
/// Published the first time a service becomes visible to the public.
///
/// PUBLIC CONTRACT: other modules subscribe to this type, so changing it is a
/// breaking change for them. Add fields, never remove or repurpose them.
/// Known subscribers today: none. Planned: Appointments, which builds its own
/// projection of title and duration from this event so a booking page can
/// name a service without querying another module.
///
/// A draft NEVER produces this event: subscribers must not learn about
/// services the public cannot see, or a booking link could exist before the
/// service does.
/// </summary>
/// <param name="ServiceId">
/// Identifier of the service. It is the ONLY thing another module should
/// store as a reference — never a foreign key, because the tables live in
/// different schemas and the modules are separately removable.
/// </param>
/// <param name="Title">Title as published.</param>
/// <param name="Slug">URL segment of the public page.</param>
/// <param name="DurationMinutes">
/// Duration of one appointment, or <c>null</c> for a service that is only
/// described. A bookable service always carries one: the domain refuses to
/// publish a bookable service without it.
/// </param>
/// <param name="IsBookable">
/// Whether the service accepts new bookings RIGHT NOW — the effective value,
/// not just the flag: an unpublished or archived service is never bookable.
/// A subscriber can therefore honour it without knowing this module's
/// publication rules.
/// </param>
public sealed record ServicePublished(
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

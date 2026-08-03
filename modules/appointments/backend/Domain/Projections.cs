using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// This module's copy of a bookable service.
///
/// It is a PROJECTION, not the source of truth: the service lives in the
/// Services module, and this row is created and updated by subscribing to its
/// public events. Duplicating the title and the duration is deliberate — it
/// lets a booking page name a service, and the availability engine size a
/// slot, without either reaching into another module's schema. That is what
/// keeps the two separately installable.
///
/// It also means an appointment taken last month still shows the name the
/// service had, even if it was renamed since: the copy is refreshed by
/// events, and the events say what changed and when.
/// </summary>
public sealed class ServiceProjection : EntityBase<Guid>
{
    private ServiceProjection() { }

    /// <summary>Title of the service, as last announced.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>URL segment of its public page, used to build booking links.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Length of one appointment. Null for a service that is only described —
    /// which is also a service that cannot be booked.
    /// </summary>
    public int? DurationMinutes { get; private set; }

    /// <summary>
    /// Whether the service accepts new bookings right now.
    ///
    /// The EFFECTIVE value announced by the Services module: it is already
    /// false for a draft or an archived service, so this module never has to
    /// re-implement another module's publication rules.
    /// </summary>
    public bool IsBookable { get; private set; }

    /// <summary>Whether the service was withdrawn from the catalogue.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Whether a new appointment may be taken for this service.
    ///
    /// A duration is part of the answer, not an implementation detail: without
    /// one there is nothing to size a slot with, so the booking page would
    /// have nothing to offer.
    /// </summary>
    public bool CanBeBooked => IsBookable && !IsArchived && DurationMinutes is > 0;

    /// <summary>
    /// Creates the projection for a service announced as published.
    /// </summary>
    /// <param name="serviceId">Identifier from the Services module.</param>
    /// <param name="title">Title as announced.</param>
    /// <param name="slug">URL segment of its page.</param>
    /// <param name="durationMinutes">Length of one appointment, or null.</param>
    /// <param name="isBookable">Whether it accepts bookings.</param>
    /// <returns>The projection row.</returns>
    public static ServiceProjection FromPublication(
        Guid serviceId,
        string title,
        string slug,
        int? durationMinutes,
        bool isBookable
    ) =>
        new()
        {
            Id = serviceId,
            Title = title,
            Slug = slug,
            DurationMinutes = durationMinutes,
            IsBookable = isBookable,
        };

    /// <summary>
    /// Applies an announced change.
    /// </summary>
    /// <param name="title">Title after the change.</param>
    /// <param name="slug">URL segment after the change.</param>
    /// <param name="durationMinutes">Length of one appointment, or null.</param>
    /// <param name="isBookable">Whether it accepts bookings.</param>
    public void Apply(string title, string slug, int? durationMinutes, bool isBookable)
    {
        Title = title;
        Slug = slug;
        DurationMinutes = durationMinutes;
        IsBookable = isBookable;
    }

    /// <summary>Records that the service was withdrawn from the catalogue.</summary>
    public void Archive()
    {
        IsArchived = true;
        IsBookable = false;
    }
}

/// <summary>
/// This module's copy of the person who books.
///
/// A projection from the Auth module's registration event, for the same
/// reason as <see cref="ServiceProjection"/>: the public events of this
/// module carry the customer's email and name so a subscriber can write to
/// them, and a subscriber must never have to query another module to find out
/// who to write to.
/// </summary>
public sealed class CustomerProjection : EntityBase<Guid>
{
    private CustomerProjection() { }

    /// <summary>Email of the account, mirrored from Auth.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Name to address the customer by, mirrored from Auth.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// The phone number this customer last left on a booking, if any.
    ///
    /// Kept so the next booking form arrives filled in. It is a CONVENIENCE
    /// and never a source of truth: every appointment carries its own
    /// <see cref="Appointment.ContactPhone"/>, because someone booking for a
    /// relative legitimately leaves a different number.
    /// </summary>
    public string? LastContactPhone { get; private set; }

    /// <summary>
    /// Creates the projection for a newly registered account.
    /// </summary>
    /// <param name="userId">Account identifier from Auth.</param>
    /// <param name="email">Email of the account.</param>
    /// <param name="displayName">Name shown in the UI.</param>
    /// <returns>The projection row.</returns>
    public static CustomerProjection FromRegistration(
        Guid userId,
        string email,
        string displayName
    ) => new() { Id = userId, Email = email, DisplayName = displayName };

    /// <summary>
    /// Remembers the phone number used on a booking.
    /// </summary>
    /// <param name="phone">The number the customer just left.</param>
    public void RememberPhone(string phone) => LastContactPhone = phone.Trim();
}

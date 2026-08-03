using EnterpriseFramework.Domain.Common;
using EnterpriseFramework.Modules.Services.Contracts.Events;

namespace EnterpriseFramework.Modules.Services.Domain;

/// <summary>
/// One entry of the catalogue: something the organization offers, described
/// on the public site and — once the Appointments module exists — bookable.
///
/// Responsibilities:
/// - Holds the editorial content (titles, descriptions, images) and the
///   practical data (duration, price).
/// - Decides which public event a change produces, because "did this change
///   need announcing?" is a question about the TRANSITION, and the transition
///   is only fully known here.
/// - Keeps the gallery coherent: there is always a cover while there is at
///   least one image.
///
/// It contains no persistence logic (EF Core configuration lives in
/// <c>ServicesDbContext</c>), no authorization logic (the endpoints do that)
/// and no input validation: the shape of a request is checked by the
/// FluentValidation validators of the commands — in particular the
/// "bookable implies a duration" rule, which the form must be able to report
/// next to the offending field.
/// </summary>
public sealed class Service : EntityBase<Guid>
{
    private readonly List<ServiceImage> _images = [];

    private Service() { }

    /// <summary>Name of the service, shown as the heading of its page.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// URL segment of the public page, unique across the catalogue.
    ///
    /// Derived from the title but stored and editable: once a page has been
    /// shared, its address belongs to whoever bookmarked it, so it must be
    /// able to survive a rewritten title.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// One line, used on the cards of the showcase list.
    ///
    /// Separate from <see cref="Description"/> on purpose: truncating a long
    /// text at a fixed length cuts it mid-sentence, and no amount of CSS
    /// fixes a sentence that stops at "che permette di".
    /// </summary>
    public string ShortDescription { get; private set; } = string.Empty;

    /// <summary>Long text of the detail page. Plain text, not markup.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Length of one appointment, in minutes, or <c>null</c> for a service
    /// that is only described.
    ///
    /// It is what will generate the bookable slots, which is why
    /// <see cref="IsBookable"/> is rejected without it.
    /// </summary>
    public int? DurationMinutes { get; private set; }

    /// <summary>Price, or <c>null</c> when the price is not published.</summary>
    public decimal? Price { get; private set; }

    /// <summary>
    /// ISO 4217 code of <see cref="Price"/>, or <c>null</c> when there is no
    /// price. An amount without a currency is not information.
    /// </summary>
    public string? Currency { get; private set; }

    /// <summary>Whether the service is visible to anonymous visitors.</summary>
    public bool IsPublished { get; private set; }

    /// <summary>
    /// Whether the service accepts bookings. Some services are only told
    /// about — an assessment, a partnership — and have nothing to reserve.
    /// </summary>
    public bool IsBookable { get; private set; }

    /// <summary>Position in the showcase; lower comes first.</summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether the service has been withdrawn from the catalogue.
    ///
    /// Withdrawn, not deleted: another module may reference this identifier
    /// (an appointment does), and this module cannot know. See
    /// <see cref="Archive"/>.
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>Images of the service; the gallery order is <c>SortOrder</c>.</summary>
    public IReadOnlyCollection<ServiceImage> Images => _images.AsReadOnly();

    /// <summary>UTC instant the service was created.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>UTC instant of the last change.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Whether the service accepts new bookings right now.
    ///
    /// The EFFECTIVE answer, not the flag: a draft or an archived service is
    /// never bookable, whatever <see cref="IsBookable"/> says. It is the value
    /// that travels in the public events, so a subscriber never has to
    /// re-implement this module's publication rules.
    /// </summary>
    public bool AcceptsBookings => IsBookable && IsPublished && !IsArchived;

    /// <summary>
    /// Creates a service.
    /// </summary>
    /// <param name="state">Validated editable state of the new service.</param>
    /// <returns>The new service, carrying its publication event when published.</returns>
    public static Service Create(ServiceState state)
    {
        var now = DateTime.UtcNow;
        var service = new Service { Id = Guid.NewGuid(), CreatedAtUtc = now, UpdatedAtUtc = now };

        service.Apply(state);

        if (service.IsPublished)
        {
            service.RaiseDomainEvent(
                new ServicePublished(
                    service.Id,
                    service.Title,
                    service.Slug,
                    service.DurationMinutes,
                    service.AcceptsBookings
                )
            );
        }

        return service;
    }

    /// <summary>
    /// Applies an edit and raises the event the transition calls for.
    ///
    /// Which event depends on where the service WAS, not on where it lands: a
    /// draft becoming visible is a publication, anything else that touches an
    /// already-public service is an update — including the edit that
    /// withdraws it, which subscribers must hear about so bookings stop. A
    /// draft edited into another draft announces nothing at all, because
    /// nobody outside this module may know it exists.
    /// </summary>
    /// <param name="state">Validated editable state to apply.</param>
    public void Update(ServiceState state)
    {
        var wasPublished = IsPublished;

        Apply(state);
        UpdatedAtUtc = DateTime.UtcNow;

        if (!wasPublished && IsPublished)
        {
            RaiseDomainEvent(
                new ServicePublished(Id, Title, Slug, DurationMinutes, AcceptsBookings)
            );
        }
        else if (wasPublished)
        {
            RaiseDomainEvent(new ServiceUpdated(Id, Title, Slug, DurationMinutes, AcceptsBookings));
        }
    }

    /// <summary>
    /// Withdraws the service from the catalogue.
    ///
    /// Idempotent: archiving an archived service raises nothing and is not an
    /// error, because the caller's goal already holds.
    /// </summary>
    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ServiceArchived(Id));
    }

    /// <summary>
    /// Attaches an image.
    ///
    /// The first image of a service becomes its cover automatically: a list of
    /// cards with one missing thumbnail looks broken, and "remember to tick
    /// the cover box" is not a design.
    /// </summary>
    /// <param name="storageFileId">Identifier of the file held by the Storage module.</param>
    /// <param name="altText">Text alternative; required, see <see cref="ServiceImage"/>.</param>
    /// <param name="sortOrder">Position in the gallery.</param>
    /// <returns>The attached image.</returns>
    public ServiceImage AddImage(Guid storageFileId, string altText, int sortOrder)
    {
        var image = ServiceImage.Attach(
            Id,
            storageFileId,
            altText,
            sortOrder,
            isCover: _images.Count == 0
        );

        _images.Add(image);
        UpdatedAtUtc = DateTime.UtcNow;

        return image;
    }

    /// <summary>
    /// Detaches an image.
    ///
    /// Removing the cover promotes the next one, for the same reason the
    /// first image was promoted on arrival.
    /// </summary>
    /// <param name="imageId">Identifier of the image to remove.</param>
    /// <returns>
    /// The removed image, or <c>null</c> when it was not attached — the caller
    /// wanted it gone, and it is.
    /// </returns>
    public ServiceImage? RemoveImage(Guid imageId)
    {
        var image = _images.Find(candidate => candidate.Id == imageId);

        if (image is null)
        {
            return null;
        }

        _images.Remove(image);

        if (image.IsCover)
        {
            _images.OrderBy(remaining => remaining.SortOrder).FirstOrDefault()?.MakeCover();
        }

        UpdatedAtUtc = DateTime.UtcNow;

        return image;
    }

    /// <summary>
    /// Chooses which image represents the service.
    ///
    /// Demoting the previous cover is done here rather than by the caller
    /// because "exactly one cover" is an invariant of the gallery, and an
    /// invariant enforced by convention is enforced by nobody.
    /// </summary>
    /// <param name="imageId">Identifier of the image to promote.</param>
    /// <returns><c>true</c> when the image belongs to this service.</returns>
    public bool SetCover(Guid imageId)
    {
        var image = _images.Find(candidate => candidate.Id == imageId);

        if (image is null)
        {
            return false;
        }

        foreach (var other in _images)
        {
            other.ClearCover();
        }

        image.MakeCover();
        UpdatedAtUtc = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Assigns the editable state. Shared by creation and edit so the two
    /// paths cannot drift into normalizing their input differently.
    /// </summary>
    private void Apply(ServiceState state)
    {
        Title = state.Title.Trim();
        Slug = state.Slug.Trim();
        ShortDescription = state.ShortDescription.Trim();
        Description = state.Description.Trim();
        DurationMinutes = state.DurationMinutes;
        Price = state.Price;
        // A currency without an amount is noise in the database and a "EUR"
        // with nothing next to it on screen.
        Currency = state.Price is null ? null : state.Currency?.Trim().ToUpperInvariant();
        IsPublished = state.IsPublished;
        IsBookable = state.IsBookable;
        SortOrder = state.SortOrder;
    }
}

/// <summary>
/// The editable state of a <see cref="Service"/>, passed as one value so
/// creation and edit cannot fall out of step and so a caller cannot swap two
/// adjacent flags without the compiler noticing.
/// </summary>
/// <param name="Title">Name of the service.</param>
/// <param name="Slug">URL segment; already normalized and known to be unique.</param>
/// <param name="ShortDescription">One line for the list card.</param>
/// <param name="Description">Long text of the detail page.</param>
/// <param name="DurationMinutes">Length of one appointment, or null.</param>
/// <param name="Price">Price, or null when it is not published.</param>
/// <param name="Currency">ISO 4217 code of the price; ignored when there is no price.</param>
/// <param name="IsPublished">Whether visitors can see it.</param>
/// <param name="IsBookable">Whether it accepts bookings.</param>
/// <param name="SortOrder">Position in the showcase.</param>
public sealed record ServiceState(
    string Title,
    string Slug,
    string ShortDescription,
    string Description,
    int? DurationMinutes,
    decimal? Price,
    string? Currency,
    bool IsPublished,
    bool IsBookable,
    int SortOrder
);

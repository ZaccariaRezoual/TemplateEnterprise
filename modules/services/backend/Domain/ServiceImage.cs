using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Services.Domain;

/// <summary>
/// One picture of a service.
///
/// It holds a REFERENCE to a file owned by the Storage module, never the
/// bytes and never a path: the two modules keep separate schemas, so the link
/// is an identifier the browser resolves against Storage's own endpoint. That
/// file must have been uploaded as public, or the showcase would ask an
/// anonymous visitor for a token.
/// </summary>
public sealed class ServiceImage : EntityBase<Guid>
{
    private ServiceImage() { }

    /// <summary>Service this image belongs to.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Identifier of the underlying file in the Storage module.</summary>
    public Guid StorageFileId { get; private set; }

    /// <summary>
    /// Text alternative, always present.
    ///
    /// Required by the accessibility contract of the design system, not as a
    /// courtesy: an image without one is simply absent for anyone who does
    /// not see it, and a link preview has nothing to describe. Making it
    /// mandatory at the model level is the only way it does not become the
    /// field everybody skips.
    /// </summary>
    public string AltText { get; private set; } = string.Empty;

    /// <summary>Position in the gallery; lower comes first.</summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether this is the image used on the list card and in link previews.
    /// Exactly one image of a service carries it while the gallery is not
    /// empty.
    /// </summary>
    public bool IsCover { get; private set; }

    /// <summary>
    /// Attaches an uploaded file to a service.
    /// </summary>
    /// <param name="serviceId">Service the image belongs to.</param>
    /// <param name="storageFileId">Identifier of the uploaded file.</param>
    /// <param name="altText">Text alternative.</param>
    /// <param name="sortOrder">Position in the gallery.</param>
    /// <param name="isCover">Whether it is the cover.</param>
    /// <returns>The attached image.</returns>
    internal static ServiceImage Attach(
        Guid serviceId,
        Guid storageFileId,
        string altText,
        int sortOrder,
        bool isCover
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            StorageFileId = storageFileId,
            AltText = altText.Trim(),
            SortOrder = sortOrder,
            IsCover = isCover,
        };

    /// <summary>
    /// Makes this image the cover. Called by the owning service, which is the
    /// only place that can also demote the previous one.
    /// </summary>
    internal void MakeCover() => IsCover = true;

    /// <summary>Stops this image from being the cover.</summary>
    internal void ClearCover() => IsCover = false;
}

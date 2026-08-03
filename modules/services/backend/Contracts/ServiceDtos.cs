using EnterpriseFramework.Modules.Services.Domain;

namespace EnterpriseFramework.Modules.Services.Contracts;

/// <summary>
/// A service as the ADMINISTRATION sees it: every field, including the ones
/// that decide whether the public sees anything at all.
///
/// It is a separate shape from <see cref="PublicServiceDto"/> on purpose.
/// Sharing one DTO between the two audiences means a new administrative field
/// is published to anonymous visitors by default, and nothing fails when that
/// happens.
/// </summary>
/// <param name="Id">Identifier of the service.</param>
/// <param name="Title">Name of the service.</param>
/// <param name="Slug">URL segment of the public page.</param>
/// <param name="ShortDescription">One line, used on the showcase cards.</param>
/// <param name="Description">Long text of the detail page.</param>
/// <param name="DurationMinutes">Length of one appointment, or null.</param>
/// <param name="Price">Price, or null when it is not published.</param>
/// <param name="Currency">ISO 4217 code of the price, or null.</param>
/// <param name="IsPublished">Whether visitors can see it.</param>
/// <param name="IsBookable">Whether it accepts bookings.</param>
/// <param name="SortOrder">Position in the showcase; lower comes first.</param>
/// <param name="IsArchived">Whether it has been withdrawn from the catalogue.</param>
/// <param name="Images">Gallery, ordered as it is shown.</param>
/// <param name="CreatedAtUtc">When it was created.</param>
/// <param name="UpdatedAtUtc">When it last changed.</param>
public sealed record AdminServiceDto(
    Guid Id,
    string Title,
    string Slug,
    string ShortDescription,
    string Description,
    int? DurationMinutes,
    decimal? Price,
    string? Currency,
    bool IsPublished,
    bool IsBookable,
    int SortOrder,
    bool IsArchived,
    IReadOnlyList<ServiceImageDto> Images,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
)
{
    /// <summary>
    /// Maps an entity to its administrative representation.
    /// </summary>
    /// <param name="service">The entity to map; its images must be loaded.</param>
    /// <returns>The DTO.</returns>
    public static AdminServiceDto FromService(Service service) =>
        new(
            service.Id,
            service.Title,
            service.Slug,
            service.ShortDescription,
            service.Description,
            service.DurationMinutes,
            service.Price,
            service.Currency,
            service.IsPublished,
            service.IsBookable,
            service.SortOrder,
            service.IsArchived,
            [.. service.Images.OrderBy(image => image.SortOrder).Select(ServiceImageDto.FromImage)],
            service.CreatedAtUtc,
            service.UpdatedAtUtc
        );
}

/// <summary>
/// A service as an anonymous visitor sees it.
///
/// It carries no publication state at all: everything reachable through the
/// public endpoints is, by construction, published and not archived, so
/// there is no flag a caller could try to flip.
/// </summary>
/// <param name="Id">Identifier of the service, used by the booking flow.</param>
/// <param name="Title">Name of the service.</param>
/// <param name="Slug">URL segment of its page.</param>
/// <param name="ShortDescription">One line, used on the showcase cards.</param>
/// <param name="Description">Long text of the detail page.</param>
/// <param name="DurationMinutes">Length of one appointment, or null.</param>
/// <param name="Price">Price, or null when it is not published.</param>
/// <param name="Currency">ISO 4217 code of the price, or null.</param>
/// <param name="IsBookable">Whether the service accepts bookings.</param>
/// <param name="Images">Gallery, ordered as it is shown; the cover comes first.</param>
public sealed record PublicServiceDto(
    Guid Id,
    string Title,
    string Slug,
    string ShortDescription,
    string Description,
    int? DurationMinutes,
    decimal? Price,
    string? Currency,
    bool IsBookable,
    IReadOnlyList<ServiceImageDto> Images
)
{
    /// <summary>
    /// Maps an entity to its public representation.
    /// </summary>
    /// <param name="service">The entity to map; its images must be loaded.</param>
    /// <returns>The DTO.</returns>
    public static PublicServiceDto FromService(Service service) =>
        new(
            service.Id,
            service.Title,
            service.Slug,
            service.ShortDescription,
            service.Description,
            service.DurationMinutes,
            service.Price,
            service.Currency,
            // The EFFECTIVE value: a visitor must not be offered a booking
            // that the domain would then refuse.
            service.AcceptsBookings,
            [
                .. service
                    .Images.OrderByDescending(image => image.IsCover)
                    .ThenBy(image => image.SortOrder)
                    .Select(ServiceImageDto.FromImage),
            ]
        );
}

/// <summary>
/// One picture of a service, as exposed to any client.
/// </summary>
/// <param name="Id">Identifier of the association, used to detach it.</param>
/// <param name="StorageFileId">
/// Identifier of the file in the Storage module. The client builds the image
/// URL from it against Storage's public endpoint; this module never serves
/// bytes.
/// </param>
/// <param name="AltText">Text alternative; never empty.</param>
/// <param name="SortOrder">Position in the gallery.</param>
/// <param name="IsCover">Whether it is the image used on cards and previews.</param>
public sealed record ServiceImageDto(
    Guid Id,
    Guid StorageFileId,
    string AltText,
    int SortOrder,
    bool IsCover
)
{
    /// <summary>
    /// Maps an image entity to its client representation.
    /// </summary>
    /// <param name="image">The entity to map.</param>
    /// <returns>The DTO.</returns>
    public static ServiceImageDto FromImage(ServiceImage image) =>
        new(image.Id, image.StorageFileId, image.AltText, image.SortOrder, image.IsCover);
}

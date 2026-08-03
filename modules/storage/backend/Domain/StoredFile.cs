using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Storage.Domain;

/// <summary>
/// Metadata of an uploaded file. The bytes live in the storage provider; this
/// row is the only thing that maps a public id to them.
/// </summary>
public sealed class StoredFile : EntityBase<Guid>
{
    private StoredFile() { }

    /// <summary>Name as uploaded, shown on download. Never used as a path.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Content type DECLARED by the uploader. Kept as metadata and never
    /// served: it is a string anyone can set. See <see cref="SafeContentType"/>.
    /// </summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// Content type the file is actually served with, decided from its first
    /// bytes at upload time by <see cref="ContentSniffer"/>.
    ///
    /// A recognized image keeps its real type so a browser can render it; a
    /// file we do not recognize is <c>application/octet-stream</c>, which a
    /// browser downloads instead of executing.
    /// </summary>
    public string SafeContentType { get; private set; } =
        ContentSniffer.OpaqueContentType;

    /// <summary>
    /// Who may download the file. Decided at upload; private by default.
    /// </summary>
    public FileVisibility Visibility { get; private set; } = FileVisibility.Private;

    /// <summary>Size in bytes.</summary>
    public long SizeInBytes { get; private set; }

    /// <summary>Opaque key identifying the bytes inside the storage provider.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    /// <summary>Account that uploaded the file.</summary>
    public Guid UploadedByUserId { get; private set; }

    /// <summary>UTC instant of the upload.</summary>
    public DateTime UploadedAtUtc { get; private set; }

    /// <summary>
    /// Records an uploaded file.
    /// </summary>
    /// <param name="fileName">Name as uploaded.</param>
    /// <param name="contentType">Content type DECLARED by the uploader; stored, never served.</param>
    /// <param name="safeContentType">
    /// Content type detected from the bytes, used when serving. See
    /// <see cref="ContentSniffer"/>.
    /// </param>
    /// <param name="sizeInBytes">Size in bytes.</param>
    /// <param name="storageKey">Key returned by the storage provider.</param>
    /// <param name="uploadedByUserId">Account that uploaded it.</param>
    /// <param name="visibility">
    /// Who may download it. The caller must pass this explicitly — a
    /// parameter with a default is a parameter that gets forgotten, and the
    /// value being forgotten here decides whether the internet can read the
    /// file.
    /// </param>
    /// <returns>The metadata row.</returns>
    public static StoredFile Record(
        string fileName,
        string contentType,
        string safeContentType,
        long sizeInBytes,
        string storageKey,
        Guid uploadedByUserId,
        FileVisibility visibility
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            SafeContentType = safeContentType,
            SizeInBytes = sizeInBytes,
            StorageKey = storageKey,
            UploadedByUserId = uploadedByUserId,
            Visibility = visibility,
            UploadedAtUtc = DateTime.UtcNow,
        };
}

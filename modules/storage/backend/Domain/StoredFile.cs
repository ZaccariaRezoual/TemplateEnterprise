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

    /// <summary>Content type served on download.</summary>
    public string ContentType { get; private set; } = string.Empty;

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
    /// <param name="contentType">Content type to serve on download.</param>
    /// <param name="sizeInBytes">Size in bytes.</param>
    /// <param name="storageKey">Key returned by the storage provider.</param>
    /// <param name="uploadedByUserId">Account that uploaded it.</param>
    /// <returns>The metadata row.</returns>
    public static StoredFile Record(
        string fileName,
        string contentType,
        long sizeInBytes,
        string storageKey,
        Guid uploadedByUserId
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            SizeInBytes = sizeInBytes,
            StorageKey = storageKey,
            UploadedByUserId = uploadedByUserId,
            UploadedAtUtc = DateTime.UtcNow,
        };
}

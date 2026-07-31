namespace EnterpriseFramework.Modules.Storage.Abstractions;

/// <summary>
/// Where file bytes physically live.
///
/// The abstraction exists so the same application runs on local disk in
/// development and on S3-compatible object storage in production, with no
/// change above this interface. Note what it does NOT expose: paths. Callers
/// work with opaque storage KEYS, so no caller can build a filesystem path,
/// which is how directory-traversal bugs get in.
/// </summary>
public interface IFileStorageProvider
{
    /// <summary>
    /// Stores a stream and returns the key needed to read it back.
    /// </summary>
    /// <param name="content">Stream to store; read to the end.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An opaque storage key.</returns>
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a stored file for reading.
    /// </summary>
    /// <param name="storageKey">Key returned by <see cref="SaveAsync"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A readable stream; the caller disposes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the key does not exist.</exception>
    Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a stored file. Deleting an unknown key is not an error.
    /// </summary>
    /// <param name="storageKey">Key returned by <see cref="SaveAsync"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

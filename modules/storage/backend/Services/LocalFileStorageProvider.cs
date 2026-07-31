using EnterpriseFramework.Modules.Storage.Abstractions;
using EnterpriseFramework.Modules.Storage.Options;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Storage.Services;

/// <summary>
/// Stores files on the local filesystem under a configured root.
///
/// Security properties:
/// - Keys are GENERATED here (a GUID), never derived from the uploaded file
///   name, so a name like "../../appsettings.json" can never become a path.
/// - Every key is still validated and the resolved path re-checked against
///   the root before any I/O: defence in depth, because a future caller may
///   pass a key that came from the database or a URL.
///
/// Suitable for development and single-instance deployments. Multiple
/// instances need shared storage — swap in an object-storage provider.
/// </summary>
public sealed class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string _rootPath;

    /// <summary>
    /// Initializes the provider and ensures the root directory exists.
    /// </summary>
    /// <param name="options">Storage configuration (root path).</param>
    public LocalFileStorageProvider(IOptions<StorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.LocalRootPath);
        Directory.CreateDirectory(_rootPath);
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        // The key is ours, not the uploader's: file names never reach the
        // filesystem.
        var storageKey = Guid.NewGuid().ToString("N");

        await using var target = File.Create(ResolvePath(storageKey));
        await content.CopyToAsync(target, cancellationToken).ConfigureAwait(false);

        return storageKey;
    }

    /// <inheritdoc />
    public Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        return File.Exists(path)
            ? Task.FromResult<Stream>(File.OpenRead(path))
            : throw new FileNotFoundException("Stored file not found.", storageKey);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        File.Delete(ResolvePath(storageKey));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Turns a storage key into an absolute path, refusing anything that
    /// could escape the root.
    /// </summary>
    /// <param name="storageKey">The key to resolve.</param>
    /// <returns>The absolute path inside the storage root.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the key is not a plain identifier or resolves outside the
    /// root — the two shapes a traversal attempt takes.
    /// </exception>
    private string ResolvePath(string storageKey)
    {
        if (
            string.IsNullOrWhiteSpace(storageKey)
            || !storageKey.All(char.IsAsciiLetterOrDigit)
        )
        {
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));

        // Even with the character check above: never trust a single guard for
        // a filesystem boundary.
        if (!path.StartsWith(_rootPath, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid storage key.", nameof(storageKey));
        }

        return path;
    }
}

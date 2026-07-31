namespace EnterpriseFramework.Modules.Storage.Options;

/// <summary>
/// Typed configuration of the Storage module (section "Storage").
/// </summary>
public sealed class StorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Storage";

    /// <summary>Directory holding stored files for the local provider.</summary>
    public string LocalRootPath { get; init; } = "storage-files";

    /// <summary>
    /// Largest accepted upload, in bytes. Enforced before the body is read:
    /// an unbounded upload endpoint is a denial-of-service vector.
    /// </summary>
    public long MaxUploadBytes { get; init; } = 10 * 1024 * 1024;
}

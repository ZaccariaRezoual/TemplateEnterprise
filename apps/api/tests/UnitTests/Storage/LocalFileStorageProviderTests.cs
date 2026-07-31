using System.Text;
using EnterpriseFramework.Modules.Storage.Options;
using EnterpriseFramework.Modules.Storage.Services;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Storage;

/// <summary>
/// The storage provider turns opaque keys into filesystem paths, which is
/// exactly where directory traversal lives. These tests are the guard on that
/// boundary, so they are as much about what the provider REFUSES as what it
/// stores.
/// </summary>
public sealed class LocalFileStorageProviderTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"ef-storage-{Guid.NewGuid():N}"
    );

    private LocalFileStorageProvider CreateProvider() =>
        new(Microsoft.Extensions.Options.Options.Create(new StorageOptions { LocalRootPath = _root }));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAndOpen_RoundTripsTheContent()
    {
        var provider = CreateProvider();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello storage"));

        var key = await provider.SaveAsync(content);

        await using var stored = await provider.OpenAsync(key);
        using var reader = new StreamReader(stored);
        (await reader.ReadToEndAsync()).ShouldBe("hello storage");
    }

    [Fact]
    public async Task Save_GeneratesTheKeyItself()
    {
        var provider = CreateProvider();
        using var first = new MemoryStream([1]);
        using var second = new MemoryStream([2]);

        var firstKey = await provider.SaveAsync(first);
        var secondKey = await provider.SaveAsync(second);

        // Keys come from the provider, never from a file name, so an
        // uploaded "../../secret" can never become a path.
        firstKey.ShouldNotBe(secondKey);
        firstKey.All(char.IsAsciiLetterOrDigit).ShouldBeTrue();
    }

    [Theory]
    [InlineData("../appsettings")]
    [InlineData("..\\appsettings")]
    [InlineData("sub/dir")]
    [InlineData("with.dot")]
    [InlineData("with-dash")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Open_RefusesAnyKeyThatIsNotAPlainIdentifier(string key)
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentException>(() => provider.OpenAsync(key));
        await Should.ThrowAsync<ArgumentException>(() => provider.DeleteAsync(key));
    }

    [Fact]
    public async Task Open_ThrowsForAnUnknownButWellFormedKey()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<FileNotFoundException>(
            () => provider.OpenAsync(Guid.NewGuid().ToString("N"))
        );
    }

    [Fact]
    public async Task Delete_IsIdempotent()
    {
        var provider = CreateProvider();
        using var content = new MemoryStream([1]);
        var key = await provider.SaveAsync(content);

        await provider.DeleteAsync(key);

        // Deleting again is not an error: the caller's goal already holds.
        await Should.NotThrowAsync(() => provider.DeleteAsync(key));
    }
}

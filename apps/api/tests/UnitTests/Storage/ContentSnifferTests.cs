using EnterpriseFramework.Modules.Storage.Domain;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Storage;

/// <summary>
/// What a stored file is served as.
///
/// This is a security test dressed as a formatting test: the content type is
/// what decides whether a browser renders a payload on our own origin, and
/// the whole point of the sniffer is that the uploader does not get a vote.
/// </summary>
public sealed class ContentSnifferTests
{
    private static readonly byte[] Png =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    [Fact]
    public void RecognizesPng()
    {
        ContentSniffer.Detect(Png).ShouldBe("image/png");
    }

    [Fact]
    public void RecognizesJpeg()
    {
        ContentSniffer.Detect([0xFF, 0xD8, 0xFF, 0xE0]).ShouldBe("image/jpeg");
    }

    [Fact]
    public void RecognizesGif()
    {
        ContentSniffer.Detect("GIF89a"u8).ShouldBe("image/gif");
    }

    [Fact]
    public void RecognizesWebp()
    {
        // RIFF container with the WEBP form type at offset 8.
        byte[] webp = [.. "RIFF"u8, 0x1A, 0x00, 0x00, 0x00, .. "WEBP"u8];

        ContentSniffer.Detect(webp).ShouldBe("image/webp");
    }

    [Fact]
    public void RecognizesAvif()
    {
        byte[] avif = [0x00, 0x00, 0x00, 0x1C, .. "ftyp"u8, .. "avif"u8];

        ContentSniffer.Detect(avif).ShouldBe("image/avif");
    }

    [Fact]
    public void TreatsHtmlAsOpaqueBytes()
    {
        // The scenario the sniffer exists for: an .html uploaded as
        // "photo.png". Served as octet-stream it is downloaded, not executed.
        ContentSniffer.Detect("<!doctype html><script>"u8).ShouldBe(ContentSniffer.OpaqueContentType);
    }

    [Fact]
    public void TreatsSvgAsOpaqueBytes()
    {
        // Deliberately not in the allowlist: an SVG is a document that can
        // carry script, and serving one inline from our origin is stored XSS.
        ContentSniffer.Detect("<svg xmlns=\"http://www.w3.org/2000/svg\">"u8)
            .ShouldBe(ContentSniffer.OpaqueContentType);
    }

    [Fact]
    public void TreatsAnEmptyFileAsOpaqueBytes()
    {
        ContentSniffer.Detect([]).ShouldBe(ContentSniffer.OpaqueContentType);
    }

    [Fact]
    public async Task ReadsOnlyTheHeaderOfTheStream()
    {
        using var stream = new MemoryStream([.. Png, .. new byte[4096]]);

        var header = await ContentSniffer.ReadHeaderAsync(stream);

        ContentSniffer.Detect(header).ShouldBe("image/png");
        // Whole files can be gigabytes; the sniffer must not pull one into
        // memory to decide what it is.
        header.Length.ShouldBeLessThanOrEqualTo(16);
    }

    [Fact]
    public async Task HandlesAFileShorterThanTheHeader()
    {
        using var stream = new MemoryStream([0xFF, 0xD8]);

        var header = await ContentSniffer.ReadHeaderAsync(stream);

        ContentSniffer.Detect(header).ShouldBe(ContentSniffer.OpaqueContentType);
    }
}

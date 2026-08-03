namespace EnterpriseFramework.Modules.Storage.Domain;

/// <summary>
/// Decides the content type a stored file is SERVED with, by looking at its
/// first bytes rather than at what the uploader claimed.
///
/// Why it exists: the uploader's content type is a string anyone can set, and
/// echoing it means an uploaded <c>.html</c> renamed <c>photo.png</c> executes
/// on our own origin with the visitor's session. The module therefore keeps
/// the declared type as metadata and never serves it.
///
/// The allowlist is short on purpose. A recognized image is served inline,
/// because a showcase page has to be able to display it; everything else is
/// served as an opaque download, which no browser will execute.
/// </summary>
public static class ContentSniffer
{
    /// <summary>
    /// Content type used for anything not recognized: opaque bytes, which a
    /// browser downloads instead of rendering.
    /// </summary>
    public const string OpaqueContentType = "application/octet-stream";

    /// <summary>Longest signature we need to look at.</summary>
    private const int HeaderLength = 12;

    /// <summary>
    /// Detects the content type of a file from its leading bytes.
    /// </summary>
    /// <param name="header">
    /// The first bytes of the file. Fewer than <see cref="HeaderLength"/>
    /// bytes are fine — the file is simply not recognized.
    /// </param>
    /// <returns>
    /// The image content type when the bytes match a known image format, or
    /// <see cref="OpaqueContentType"/> otherwise.
    /// </returns>
    public static string Detect(ReadOnlySpan<byte> header)
    {
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (StartsWith(header, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return "image/png";
        }

        // JPEG: FF D8 FF
        if (StartsWith(header, [0xFF, 0xD8, 0xFF]))
        {
            return "image/jpeg";
        }

        // GIF: "GIF87a" or "GIF89a"
        if (StartsWith(header, "GIF87a"u8) || StartsWith(header, "GIF89a"u8))
        {
            return "image/gif";
        }

        // RIFF containers carry their format at offset 8: "WEBP" for images.
        if (
            header.Length >= HeaderLength
            && StartsWith(header, "RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8)
        )
        {
            return "image/webp";
        }

        // ISO-BMFF boxes ("....ftypavif"): AVIF, the format modern encoders
        // reach for and the one a project will hit first after WebP.
        if (header.Length >= HeaderLength && header[4..8].SequenceEqual("ftyp"u8))
        {
            var brand = header[8..12];
            if (brand.SequenceEqual("avif"u8) || brand.SequenceEqual("avis"u8))
            {
                return "image/avif";
            }
        }

        // SVG is deliberately absent: it is a document that can carry script,
        // and serving one inline from our origin is stored XSS with extra
        // steps. An SVG upload still works — it is just downloaded, not
        // rendered.
        return OpaqueContentType;
    }

    /// <summary>
    /// Reads the leading bytes needed by <see cref="Detect(ReadOnlySpan{byte})"/>.
    /// </summary>
    /// <param name="content">Stream positioned at the start of the file.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The bytes read; fewer than requested for a very small file.</returns>
    public static async Task<byte[]> ReadHeaderAsync(
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        var buffer = new byte[HeaderLength];
        var read = await content.ReadAtLeastAsync(
                buffer,
                HeaderLength,
                throwOnEndOfStream: false,
                cancellationToken
            )
            .ConfigureAwait(false);

        return read == HeaderLength ? buffer : buffer[..read];
    }

    /// <summary>Tells whether a header begins with a signature.</summary>
    private static bool StartsWith(ReadOnlySpan<byte> header, ReadOnlySpan<byte> signature) =>
        header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);
}

using System.Globalization;
using System.Text;

namespace EnterpriseFramework.Modules.Services.Domain;

/// <summary>
/// Turns a human title into the URL segment of its public page.
///
/// Responsibilities: normalization only. It does NOT check uniqueness — that
/// is a question about the database, answered by the command handlers, and
/// keeping it out of here is what makes the rule testable without one.
///
/// The output is deliberately restricted to <c>a-z</c>, <c>0-9</c> and single
/// hyphens: a slug ends up in a URL, in a sitemap and in whatever someone
/// pastes into a chat, and anything else there gets percent-encoded into
/// noise.
/// </summary>
public static class Slug
{
    /// <summary>Longest slug we store, matching the column width.</summary>
    public const int MaxLength = 200;

    /// <summary>
    /// Builds a slug from arbitrary text.
    ///
    /// Accented letters are folded to their base form ("Consulenza strategica
    /// à la carte" → "consulenza-strategica-a-la-carte") rather than dropped,
    /// because dropping them turns Italian and French titles into gibberish.
    /// </summary>
    /// <param name="value">Text to convert, typically the service title.</param>
    /// <returns>
    /// The slug, or an empty string when <paramref name="value"/> holds no
    /// usable character (a title made only of punctuation, for instance).
    /// The caller decides what to do with that: the command handlers reject
    /// it, because a service without a slug has no public page.
    /// </returns>
    public static string From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // FormD splits "à" into "a" + combining accent, so the accent can be
        // dropped on its own and the base letter survives.
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSeparator = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lowered = char.ToLowerInvariant(character);

            if (lowered is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                // Separators are emitted lazily so runs of spaces and
                // punctuation collapse into one hyphen, and a trailing run
                // never reaches the output at all.
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                pendingSeparator = false;
                builder.Append(lowered);
            }
            else
            {
                pendingSeparator = true;
            }
        }

        var slug = builder.ToString();

        return slug.Length <= MaxLength ? slug : slug[..MaxLength].TrimEnd('-');
    }

    /// <summary>
    /// Returns the first free variant of a slug, appending "-2", "-3", … until
    /// one is available.
    ///
    /// Used only for slugs the system DERIVED from a title. A slug the
    /// administrator typed is never silently changed: publishing
    /// "consulenza-2" to someone who asked for "consulenza" is a worse
    /// outcome than telling them the address is taken.
    ///
    /// Pure on purpose — the caller supplies the taken slugs — so the rule can
    /// be tested without a database.
    /// </summary>
    /// <param name="desired">Normalized slug to start from.</param>
    /// <param name="taken">Slugs already in use. Compared case-sensitively; slugs are lowercase by construction.</param>
    /// <returns>A slug not present in <paramref name="taken"/>.</returns>
    public static string Disambiguate(string desired, IEnumerable<string> taken)
    {
        var used = new HashSet<string>(taken, StringComparer.Ordinal);

        if (!used.Contains(desired))
        {
            return desired;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{desired}-{suffix}";

            // The suffix must fit: truncating the STEM instead keeps the slug
            // within the column and still unique, because the loop keeps
            // going until it finds a free one.
            if (candidate.Length > MaxLength)
            {
                var room = MaxLength - (candidate.Length - desired.Length);
                candidate = $"{desired[..room].TrimEnd('-')}-{suffix}";
            }

            if (used.Add(candidate))
            {
                return candidate;
            }
        }
    }
}

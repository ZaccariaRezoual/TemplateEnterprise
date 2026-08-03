using EnterpriseFramework.Modules.Services.Domain;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Services;

/// <summary>
/// The slug is the public address of a service page: once shared it is out of
/// our hands, so the rules that produce it are worth pinning down.
/// </summary>
public sealed class SlugTests
{
    [Theory]
    [InlineData("Consulenza strategica", "consulenza-strategica")]
    [InlineData("  Spazi   multipli  ", "spazi-multipli")]
    [InlineData("Punteggiatura: e/o, punti.", "punteggiatura-e-o-punti")]
    [InlineData("MAIUSCOLE", "maiuscole")]
    [InlineData("Servizio 24/7", "servizio-24-7")]
    public void NormalizesTitlesIntoUrlSegments(string title, string expected)
    {
        Slug.From(title).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Perché noi", "perche-noi")]
    [InlineData("Attività à la carte", "attivita-a-la-carte")]
    [InlineData("Über uns", "uber-uns")]
    public void FoldsAccentsInsteadOfDroppingTheLetter(string title, string expected)
    {
        // Dropping the accented character instead would turn "perché" into
        // "perch", which is how Italian and French titles become gibberish.
        Slug.From(title).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!! ??? ***")]
    public void ReturnsEmptyWhenThereIsNothingUsable(string? title)
    {
        // Empty, not an invented value: the caller decides, and the command
        // handlers refuse rather than publish a page at a made-up address.
        Slug.From(title).ShouldBeEmpty();
    }

    [Fact]
    public void NeverEndsWithASeparator()
    {
        Slug.From("Consulenza — ").ShouldBe("consulenza");
    }

    [Fact]
    public void StaysWithinTheColumnWidth()
    {
        var slug = Slug.From(new string('a', Slug.MaxLength + 50));

        slug.Length.ShouldBe(Slug.MaxLength);
    }

    [Fact]
    public void LeavesAFreeSlugAlone()
    {
        Slug.Disambiguate("consulenza", ["altro", "consulenza-fiscale"]).ShouldBe("consulenza");
    }

    [Fact]
    public void AppendsTheFirstFreeSuffixOnCollision()
    {
        Slug.Disambiguate("consulenza", ["consulenza", "consulenza-2"]).ShouldBe("consulenza-3");
    }

    [Fact]
    public void KeepsTheDisambiguatedSlugWithinTheColumnWidth()
    {
        var stem = new string('a', Slug.MaxLength);

        var result = Slug.Disambiguate(stem, [stem]);

        // The stem is truncated to make room for the suffix; the alternative
        // is a value the database refuses to store.
        result.Length.ShouldBeLessThanOrEqualTo(Slug.MaxLength);
        result.ShouldEndWith("-2");
    }
}

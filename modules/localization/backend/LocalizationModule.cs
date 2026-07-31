using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using EnterpriseFramework.Modules.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFramework.Modules.Localization;

/// <summary>
/// A locale the application supports.
/// </summary>
/// <param name="Code">BCP 47 tag, e.g. "en" or "it".</param>
/// <param name="Name">Name shown in a language picker, in that language.</param>
public sealed record LocaleDto(string Code, string Name);

/// <summary>
/// Localization module: supported locales and their translation catalogues.
///
/// Translations are EMBEDDED RESOURCES, not database rows. That makes a
/// deployment reproducible, puts translation changes through code review, and
/// keeps a missing key a build-time artifact rather than a production
/// surprise. Projects needing translator-editable content at runtime add a
/// database-backed provider behind the same endpoint.
///
/// The catalogue is served to the frontend, which feeds it to vue-i18n, so
/// server-side messages and UI strings come from one source.
/// </summary>
public sealed class LocalizationModule : IModule
{
    /// <summary>Locale used when the requested one is unknown.</summary>
    public const string DefaultLocale = "en";

    private static readonly IReadOnlyList<LocaleDto> SupportedLocales =
    [
        new("en", "English"),
        new("it", "Italiano"),
    ];

    /// <inheritdoc />
    public string Name => "Localization";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TranslationCatalogue>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/localization").WithTags("Localization");

        // Anonymous: the sign-in screen must be translated before anyone can
        // authenticate.
        group
            .MapGet("/locales", () => TypedResults.Ok(SupportedLocales))
            .WithName("localizationLocales")
            .WithSummary("Lists the supported locales.")
            .AllowAnonymous();

        group
            .MapGet(
                "/translations/{locale}",
                (string locale, TranslationCatalogue catalogue) =>
                    TypedResults.Ok(catalogue.Get(locale))
            )
            .WithName("localizationTranslations")
            .WithSummary("Returns the translation catalogue for a locale.")
            .AllowAnonymous();
    }
}

/// <summary>
/// Loads and caches the embedded translation catalogues.
///
/// Resources are read ONCE at construction: they cannot change without a
/// deployment, so re-reading them per request would be pure overhead.
/// </summary>
public sealed class TranslationCatalogue
{
    private readonly FrozenDictionary<string, FrozenDictionary<string, string>> _catalogues;

    /// <summary>
    /// Loads every embedded catalogue.
    /// </summary>
    public TranslationCatalogue()
    {
        var assembly = typeof(TranslationCatalogue).Assembly;

        // Scoped to the Resources folder: the assembly also embeds
        // module.json, which is a manifest, not a catalogue.
        _catalogues = assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.Contains(".Resources.", StringComparison.Ordinal)
                && name.EndsWith(".json", StringComparison.Ordinal)
            )
            .Select(name => (Locale: ExtractLocale(name), ResourceName: name))
            .Where(entry => entry.Locale is not null)
            .ToFrozenDictionary(
                entry => entry.Locale!,
                entry => Load(assembly, entry.ResourceName),
                StringComparer.OrdinalIgnoreCase
            );
    }

    /// <summary>
    /// Returns the catalogue for a locale.
    /// </summary>
    /// <param name="locale">Requested locale tag.</param>
    /// <returns>
    /// The translations, falling back to the default locale for an unknown
    /// tag — an untranslated UI beats an empty one.
    /// </returns>
    public IReadOnlyDictionary<string, string> Get(string locale) =>
        _catalogues.TryGetValue(locale, out var catalogue)
            ? catalogue
            : _catalogues.GetValueOrDefault(
                LocalizationModule.DefaultLocale,
                FrozenDictionary<string, string>.Empty
            );

    private static string? ExtractLocale(string resourceName)
    {
        // "EnterpriseFramework.Modules.Localization.Resources.en.json" → "en"
        var parts = resourceName.Split('.');
        return parts.Length >= 2 ? parts[^2] : null;
    }

    private static FrozenDictionary<string, string> Load(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var translations =
            JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException($"Translation resource '{resourceName}' is empty.");
        return translations.ToFrozenDictionary(StringComparer.Ordinal);
    }
}

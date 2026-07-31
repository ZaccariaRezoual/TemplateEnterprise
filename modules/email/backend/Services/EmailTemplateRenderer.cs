using System.Net;
using System.Text.RegularExpressions;

namespace EnterpriseFramework.Modules.Email.Services;

/// <summary>
/// Renders email templates by substituting {{placeholders}}.
///
/// Deliberately minimal: a full template engine is a dependency and an attack
/// surface a framework should not impose. Projects needing layouts or loops
/// swap this class for Razor or Fluid — the callers work with rendered
/// strings, so nothing else changes.
///
/// Every substituted value is HTML-ENCODED in the HTML body: user-controlled
/// data (a display name, an email) reaches these templates, and unencoded
/// output would make the email a script-injection vector in web mail clients.
/// </summary>
public static partial class EmailTemplateRenderer
{
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.None, matchTimeoutMilliseconds: 500)]
    private static partial Regex PlaceholderPattern();

    /// <summary>
    /// Renders a template for an HTML body, encoding every substituted value.
    /// </summary>
    /// <param name="template">Template text containing {{placeholders}}.</param>
    /// <param name="values">Placeholder name → value.</param>
    /// <returns>The rendered HTML.</returns>
    public static string RenderHtml(string template, IReadOnlyDictionary<string, string> values) =>
        Render(template, values, WebUtility.HtmlEncode);

    /// <summary>
    /// Renders a template for a plain-text body, substituting values verbatim.
    /// </summary>
    /// <param name="template">Template text containing {{placeholders}}.</param>
    /// <param name="values">Placeholder name → value.</param>
    /// <returns>The rendered text.</returns>
    public static string RenderText(string template, IReadOnlyDictionary<string, string> values) =>
        Render(template, values, value => value);

    private static string Render(
        string template,
        IReadOnlyDictionary<string, string> values,
        Func<string, string> encode
    ) =>
        PlaceholderPattern()
            .Replace(
                template,
                match =>
                    values.TryGetValue(match.Groups[1].Value, out var value)
                        ? encode(value)
                        // An unknown placeholder is left as-is rather than
                        // blanked: a visibly broken email is reported, an
                        // invisibly wrong one is not.
                        : match.Value
            );
}

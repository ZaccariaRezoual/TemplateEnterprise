namespace EnterpriseFramework.Modules.Site.Options;

/// <summary>
/// Typed configuration of the Site module (section "Modules:Site").
/// </summary>
public sealed class SiteOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Modules:Site";

    /// <summary>
    /// Address that receives the contact form's messages.
    ///
    /// It is the site's decision, not the Email module's: it travels in the
    /// published event so whichever module delivers the message does not need
    /// to know anything about this site.
    /// </summary>
    public string ContactRecipient { get; init; } = "info@example.com";

    /// <summary>
    /// Messages accepted per client IP inside
    /// <see cref="ContactRateLimitWindowSeconds"/>.
    ///
    /// Deliberately small: the contact endpoint is anonymous and takes free
    /// text, which makes it the most abusable surface of the whole API. A
    /// human writes one message, not five per minute.
    /// </summary>
    public int ContactPermitLimit { get; init; } = 3;

    /// <summary>Length of the rate-limiting window, in seconds.</summary>
    public int ContactRateLimitWindowSeconds { get; init; } = 300;
}

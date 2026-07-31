namespace EnterpriseFramework.Modules.Email.Options;

/// <summary>
/// Typed configuration of the Email module (section "Email").
/// </summary>
public sealed class EmailOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Email";

    /// <summary>Address messages are sent from.</summary>
    public string FromAddress { get; init; } = "no-reply@example.com";

    /// <summary>Display name shown as the sender.</summary>
    public string FromName { get; init; } = "Enterprise Framework";

    /// <summary>Application name substituted into templates.</summary>
    public string ApplicationName { get; init; } = "Enterprise Framework";
}

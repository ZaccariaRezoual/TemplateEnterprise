namespace EnterpriseFramework.Modules.Email.Options;

/// <summary>
/// How to reach an SMTP server (section "Email:Smtp").
///
/// **Configuration, deliberately not a database setting.** Host, port and
/// above all <see cref="Password"/> are secrets: a secret editable from a web
/// form lives in a table, and from there it reaches backups, replicas and
/// audit logs. It belongs in the environment or in a secret store, which is
/// also where a deployment already keeps the connection string.
///
/// Leaving <see cref="Host"/> empty is what keeps the safe default: the
/// module then registers the logging sender and no mail leaves the process.
/// Turning delivery on is therefore a configuration change and nothing else —
/// no code, no rebuild.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Email:Smtp";

    /// <summary>
    /// Server host name. **Empty means "do not send"** — the module keeps the
    /// logging transport.
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Port. 587 is submission with STARTTLS, which is what almost every
    /// provider wants today; 465 is implicit TLS, 25 is server-to-server and
    /// is not what an application should use.
    /// </summary>
    public int Port { get; init; } = 587;

    /// <summary>
    /// Whether the connection is encrypted from the first byte (port 465),
    /// rather than upgraded with STARTTLS (port 587).
    /// </summary>
    public bool UseImplicitTls { get; init; }

    /// <summary>Account to authenticate as. Empty for an unauthenticated relay.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Password or app-specific token.
    ///
    /// Never commit it. Supply it as an environment variable
    /// (<c>Email__Smtp__Password</c>) or from the platform's secret store.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// How long to wait for the server before giving up, in seconds.
    ///
    /// It matters more than it looks: the outbox drains in the background, so
    /// a hung connection does not block a user — but it does block every
    /// message queued behind it.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    // There is deliberately NO "accept any certificate" option.
    //
    // It is the first thing someone reaches for against an internal server
    // with a self-signed certificate, and the last thing anyone remembers to
    // turn off — in a template, an option like that ships to every project
    // built on it. The password travels over that connection, so disabling
    // validation makes the encryption pointless: you are still encrypted,
    // just not necessarily to the right server.
    //
    // For an internal CA the correct fix is to trust the CA in the operating
    // system's certificate store, which is where every other client on that
    // machine already looks. For a local test server (MailHog, smtp4dev,
    // Papercut) there is nothing to work around: they speak plain SMTP, and
    // development uses the logging transport anyway.

    /// <summary>Whether enough is configured to actually deliver.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

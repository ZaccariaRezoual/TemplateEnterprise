using EnterpriseFramework.Modules.Email.Abstractions;
using EnterpriseFramework.Modules.Email.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace EnterpriseFramework.Modules.Email.Services;

/// <summary>
/// Delivers email over SMTP.
///
/// Registered by the module **only when a host is configured**, so the safe
/// default — logging instead of sending — survives a deployment that forgot
/// to set it up. Turning delivery on is a configuration change and nothing
/// else.
///
/// It is called from the outbox's background drain, never from a request, so
/// a slow or unreachable mail server delays a message and never a user.
///
/// One connection per message, deliberately: a pooled SMTP connection has to
/// be reconnected, re-authenticated and health-checked anyway, and providers
/// drop idle sessions. At the volumes an application like this sends, the
/// simpler code is worth more than the saved handshake — a project sending
/// thousands an hour should be using a transactional API, not SMTP.
/// </summary>
public sealed partial class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _email;
    private readonly SmtpOptions _smtp;
    private readonly ILogger<SmtpEmailSender> _logger;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Email delivered over SMTP to {Recipient}")]
    private static partial void LogDelivered(ILogger logger, string recipient);

    /// <summary>
    /// Initializes the sender.
    /// </summary>
    /// <param name="email">Sender identity (from address and display name).</param>
    /// <param name="smtp">Server settings.</param>
    /// <param name="logger">Logger used to report deliveries.</param>
    public SmtpEmailSender(
        IOptions<EmailOptions> email,
        IOptions<SmtpOptions> smtp,
        ILogger<SmtpEmailSender> logger
    )
    {
        _email = email.Value;
        _smtp = smtp.Value;
        _logger = logger;
    }

    /// <summary>
    /// Delivers a message.
    /// </summary>
    /// <param name="message">The message, already rendered.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="MailKit.ServiceNotAuthenticatedException">
    /// Thrown when the credentials are refused. It is left to propagate: the
    /// outbox logs it, and a misconfigured mailbox must be visible in the
    /// logs rather than swallowed into silence.
    /// </exception>
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var mime = Build(message);
        using var client = new SmtpClient
        {
            Timeout = _smtp.TimeoutSeconds * 1000,
        };

        // The server certificate is validated against the operating system's
        // trust store, and there is no option to skip it — see SmtpOptions.

        // STARTTLS is REQUIRED, not "available": `StartTlsWhenAvailable` lets
        // a server that simply does not offer it downgrade the session to
        // plaintext, and the credentials go with it.
        var security = _smtp.UseImplicitTls
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_smtp.Host, _smtp.Port, security, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_smtp.UserName))
        {
            await client.AuthenticateAsync(_smtp.UserName, _smtp.Password, cancellationToken)
                .ConfigureAwait(false);
        }

        await client.SendAsync(mime, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);

        // Without the subject or the body: an email carries personal data, and
        // logs are copied, shipped and retained far more casually than a
        // mailbox.
        LogDelivered(_logger, message.To);
    }

    /// <summary>
    /// Turns our message into a MIME document.
    ///
    /// Both an HTML and a plain-text part: some clients refuse HTML, and a
    /// message with no text alternative is a strong spam signal.
    /// </summary>
    private MimeMessage Build(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_email.FromName, _email.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };

        foreach (var attachment in message.Attachments ?? [])
        {
            // The content type is parsed rather than assumed: an .ics
            // attachment arriving as application/octet-stream is a file the
            // recipient has to open by hand instead of a calendar entry their
            // client offers to add.
            body.Attachments.Add(
                attachment.FileName,
                System.Text.Encoding.UTF8.GetBytes(attachment.Content),
                ContentType.Parse(attachment.ContentType)
            );
        }

        mime.Body = body.ToMessageBody();

        // Belt and braces: BodyBuilder already sets the text part, but a
        // message built with neither body would otherwise send an empty MIME
        // document rather than fail.
        if (string.IsNullOrEmpty(message.HtmlBody) && string.IsNullOrEmpty(message.TextBody))
        {
            mime.Body = new TextPart(TextFormat.Plain) { Text = string.Empty };
        }

        return mime;
    }
}

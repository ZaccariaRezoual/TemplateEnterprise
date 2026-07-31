namespace EnterpriseFramework.Modules.Email.Abstractions;

/// <summary>
/// An email ready to be delivered.
/// </summary>
/// <param name="To">Recipient address.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="HtmlBody">Rendered HTML body.</param>
/// <param name="TextBody">
/// Plain-text alternative. Always provided: some clients refuse HTML, and a
/// missing text part is a strong spam signal.
/// </param>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>
/// Transport that actually delivers an email.
///
/// The abstraction is the point: development writes to the log, production
/// uses SMTP or a transactional provider, tests capture in memory — and no
/// calling code changes. Callers should not use it directly; they hand
/// messages to <see cref="IEmailOutbox"/> so a slow mail server never delays
/// a user's request.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Delivers a message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Accepts email for background delivery.
///
/// Sending inline would tie a user's request to a third-party mail server:
/// registration would fail because SMTP was slow. Handing the message to an
/// outbox decouples the two.
/// </summary>
public interface IEmailOutbox
{
    /// <summary>
    /// Accepts a message for delivery.
    /// </summary>
    /// <param name="message">The message to send.</param>
    void Add(EmailMessage message);
}

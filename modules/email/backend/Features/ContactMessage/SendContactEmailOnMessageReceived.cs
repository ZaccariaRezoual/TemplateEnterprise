using System.Net;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Email.Abstractions;
using EnterpriseFramework.Modules.Site.Contracts.Events;
using MediatR;

namespace EnterpriseFramework.Modules.Email.Features.ContactMessage;

/// <summary>
/// Delivers a message written on the public contact form.
///
/// Subscribes to the Site module's public contract event, so Site has no idea
/// email exists: disabling this module means the form accepts messages and
/// nothing carries them — which is why the Site README says the contact form
/// needs a subscriber to be useful.
///
/// The message goes to the OUTBOX, never sent inline, so a slow mail server
/// cannot make a visitor's submission hang or fail.
/// </summary>
public sealed class SendContactEmailOnMessageReceived
    : INotificationHandler<DomainEventNotification<ContactMessageReceived>>
{
    private readonly IEmailOutbox _outbox;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="outbox">Outbox that delivers in the background.</param>
    public SendContactEmailOnMessageReceived(IEmailOutbox outbox)
    {
        _outbox = outbox;
    }

    /// <summary>
    /// Renders the message and hands it to the outbox.
    /// </summary>
    /// <param name="notification">The wrapped contact event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<ContactMessageReceived> notification,
        CancellationToken cancellationToken
    )
    {
        var message = notification.DomainEvent;

        // Everything here was typed by an anonymous visitor, so every value
        // is HTML-encoded before it reaches the body. A contact form that
        // pastes raw input into an HTML email hands whoever reads it a
        // scripting payload — and the reader is us.
        var name = WebUtility.HtmlEncode(message.SenderName);
        var email = WebUtility.HtmlEncode(message.SenderEmail);
        var body = WebUtility.HtmlEncode(message.Body).Replace("\n", "<br>", StringComparison.Ordinal);

        _outbox.Add(
            new EmailMessage(
                message.Recipient,
                // The sender's name in the subject, so a full inbox stays
                // scannable without opening every message.
                $"Contact form: {message.SenderName}",
                $"<p><strong>{name}</strong> &lt;{email}&gt;</p><p>{body}</p>",
                $"{message.SenderName} <{message.SenderEmail}>\n\n{message.Body}"
            )
        );

        return Task.CompletedTask;
    }
}

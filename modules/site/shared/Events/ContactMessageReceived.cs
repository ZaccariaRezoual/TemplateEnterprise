using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Site.Contracts.Events;

/// <summary>
/// Published when a visitor submits the public contact form.
///
/// PUBLIC CONTRACT: other modules subscribe to this type, so changing it is a
/// breaking change for them. Add fields, never remove or repurpose them.
/// Known subscribers today: Email (delivers the message).
///
/// The Site module publishes and does not care what happens next — it does
/// not know that email exists. If no module is listening the submission is
/// accepted and then lost, which is why the Site README states plainly that
/// the contact form needs the Email module (or another subscriber) to be
/// useful.
/// </summary>
/// <param name="SenderName">Name the visitor typed. Untrusted input.</param>
/// <param name="SenderEmail">
/// Reply address the visitor typed. Validated as an address, but NOT verified:
/// nobody proved they own it, so it must never be used as an identity.
/// </param>
/// <param name="Body">Message text. Untrusted input.</param>
/// <param name="Recipient">
/// Where the message should go, from the Site module's configuration.
///
/// It travels in the event because WHO should receive contact messages is a
/// decision of the site, not of whichever module delivers them: a subscriber
/// knows how to send, the publisher knows to whom.
/// </param>
public sealed record ContactMessageReceived(
    string SenderName,
    string SenderEmail,
    string Body,
    string Recipient
) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

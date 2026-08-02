using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Site.Contracts.Events;
using EnterpriseFramework.Modules.Site.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Site.Features.Contact;

/// <summary>
/// Handles <see cref="SendContactMessageCommand"/>: drops honeypot
/// submissions and publishes <c>ContactMessageReceived</c> for the real ones.
///
/// It does not send anything. This module knows nothing about email: it
/// announces that a visitor wrote, and whichever module delivers messages
/// decides how.
/// </summary>
public sealed partial class SendContactMessageCommandHandler
    : IRequestHandler<SendContactMessageCommand>
{
    private readonly IEventBus _eventBus;
    private readonly SiteOptions _options;
    private readonly ILogger<SendContactMessageCommandHandler> _logger;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Contact submission dropped: the honeypot field was filled"
    )]
    private static partial void LogHoneypotHit(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Contact message received")]
    private static partial void LogAccepted(ILogger logger);

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="eventBus">Bus the contact event is published on.</param>
    /// <param name="options">Site configuration, holding the recipient.</param>
    /// <param name="logger">Logger used to report drops and acceptances.</param>
    public SendContactMessageCommandHandler(
        IEventBus eventBus,
        IOptions<SiteOptions> options,
        ILogger<SendContactMessageCommandHandler> logger
    )
    {
        _eventBus = eventBus;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Accepts the message.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        SendContactMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            // Silently successful: an error would tell whoever wrote the bot
            // exactly which field gave them away.
            LogHoneypotHit(_logger);
            return;
        }

        await _eventBus
            .PublishAsync(
                new ContactMessageReceived(
                    request.Name,
                    request.Email,
                    request.Body,
                    _options.ContactRecipient
                ),
                cancellationToken
            )
            .ConfigureAwait(false);

        // Deliberately without the message or the sender: a contact form
        // carries personal data, and logs are copied, shipped and retained
        // far more casually than a mailbox.
        LogAccepted(_logger);
    }
}

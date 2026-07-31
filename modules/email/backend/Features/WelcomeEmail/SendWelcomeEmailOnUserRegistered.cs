using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Auth.Contracts.Events;
using EnterpriseFramework.Modules.Email.Abstractions;
using EnterpriseFramework.Modules.Email.Options;
using EnterpriseFramework.Modules.Email.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Email.Features.WelcomeEmail;

/// <summary>
/// Sends a welcome email when an account is created.
///
/// Subscribes to Auth's public contract event, so Auth has no idea email
/// exists: disabling this module means registrations simply stop sending one.
/// The message goes to the OUTBOX, never sent inline, so a slow mail server
/// cannot make registration fail or hang.
/// </summary>
public sealed class SendWelcomeEmailOnUserRegistered
    : INotificationHandler<DomainEventNotification<UserRegistered>>
{
    private const string HtmlTemplate = """
        <p>Hi {{name}},</p>
        <p>Your {{application}} account is ready. You can sign in with {{email}}.</p>
        <p>— The {{application}} team</p>
        """;

    private const string TextTemplate = """
        Hi {{name}},

        Your {{application}} account is ready. You can sign in with {{email}}.

        — The {{application}} team
        """;

    private readonly IEmailOutbox _outbox;
    private readonly EmailOptions _options;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="outbox">Outbox that delivers in the background.</param>
    /// <param name="options">Email configuration (application name, sender).</param>
    public SendWelcomeEmailOnUserRegistered(IEmailOutbox outbox, IOptions<EmailOptions> options)
    {
        _outbox = outbox;
        _options = options.Value;
    }

    /// <summary>
    /// Renders the welcome message and hands it to the outbox.
    /// </summary>
    /// <param name="notification">The wrapped registration event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<UserRegistered> notification,
        CancellationToken cancellationToken
    )
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = notification.DomainEvent.DisplayName,
            ["email"] = notification.DomainEvent.Email,
            ["application"] = _options.ApplicationName,
        };

        _outbox.Add(
            new EmailMessage(
                notification.DomainEvent.Email,
                $"Welcome to {_options.ApplicationName}",
                EmailTemplateRenderer.RenderHtml(HtmlTemplate, values),
                EmailTemplateRenderer.RenderText(TextTemplate, values)
            )
        );

        return Task.CompletedTask;
    }
}

using EnterpriseFramework.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Demo.Features.Echo;

/// <summary>
/// In-process subscriber of <see cref="DemoEchoedEvent"/>.
///
/// Only logs the event: it exists to prove that events published through
/// <c>IEventBus</c> reach their subscribers. Real modules subscribe to events
/// of other modules exactly like this, without referencing the publisher.
/// </summary>
public sealed partial class DemoEchoedEventSubscriber
    : INotificationHandler<DomainEventNotification<DemoEchoedEvent>>
{
    private readonly ILogger<DemoEchoedEventSubscriber> _logger;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "DemoEchoedEvent received: {Text} (occurred {OccurredOnUtc:O})"
    )]
    private static partial void LogEchoed(ILogger logger, string text, DateTime occurredOnUtc);

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="logger">Logger scoped to this handler.</param>
    public DemoEchoedEventSubscriber(ILogger<DemoEchoedEventSubscriber> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the received event.
    /// </summary>
    /// <param name="notification">The wrapped domain event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(
        DomainEventNotification<DemoEchoedEvent> notification,
        CancellationToken cancellationToken
    )
    {
        LogEchoed(_logger, notification.DomainEvent.Text, notification.DomainEvent.OccurredOnUtc);
        return Task.CompletedTask;
    }
}

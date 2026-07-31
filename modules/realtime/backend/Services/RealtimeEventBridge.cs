using EnterpriseFramework.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Realtime.Services;

/// <summary>
/// The bridge from the Event Bus to connected clients.
///
/// Registered as an OPEN GENERIC handler constrained to
/// <see cref="IRealtimeEvent"/>, so it subscribes to every present and future
/// realtime event without naming one. That is what lets a module become
/// live-updating by implementing an interface: no registration in this
/// module, no reference to SignalR in that one.
///
/// A delivery failure never fails the publisher: the domain operation already
/// succeeded and was persisted, so refusing it because a WebSocket was
/// unavailable would be worse than a client refreshing a moment later.
/// </summary>
/// <typeparam name="TEvent">A domain event marked as realtime.</typeparam>
public sealed partial class RealtimeEventBridge<TEvent>
    : INotificationHandler<DomainEventNotification<TEvent>>
    where TEvent : IRealtimeEvent
{
    private readonly RealtimeDispatcher _dispatcher;
    private readonly ILogger<RealtimeEventBridge<TEvent>> _logger;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not deliver realtime event on channel {Channel}"
    )]
    private static partial void LogDeliveryFailure(
        ILogger logger,
        string channel,
        Exception exception
    );

    /// <summary>
    /// Initializes the bridge.
    /// </summary>
    /// <param name="dispatcher">Decides who receives the event.</param>
    /// <param name="logger">Reports delivery failures.</param>
    public RealtimeEventBridge(
        RealtimeDispatcher dispatcher,
        ILogger<RealtimeEventBridge<TEvent>> logger
    )
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Delivers the event to its audience.
    /// </summary>
    /// <param name="notification">The wrapped domain event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(
        DomainEventNotification<TEvent> notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _dispatcher
                .DispatchAsync(notification.DomainEvent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogDeliveryFailure(_logger, notification.DomainEvent.Channel, exception);
        }
    }
}

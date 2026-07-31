using System.Collections.Concurrent;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Domain.Common;
using MediatR;

namespace EnterpriseFramework.Infrastructure.Events;

/// <summary>
/// In-process implementation of <see cref="IEventBus"/> backed by MediatR
/// notifications.
///
/// Dispatch is by the event's RUNTIME type, not by the generic parameter the
/// caller happened to use. This matters: publishing from a loop over
/// <c>IReadOnlyCollection&lt;IDomainEvent&gt;</c> would otherwise infer
/// <c>TEvent = IDomainEvent</c> and produce a notification no subscriber
/// listens for — the event would vanish silently. Resolving the closed
/// generic type here makes both call styles behave identically.
///
/// Swapping this class for a distributed transport (broker, Redis, SignalR
/// fan-out) requires no change in the modules that publish events.
/// </summary>
public sealed class MediatREventBus : IEventBus
{
    // Reflection is done once per event type, not once per publish.
    private static readonly ConcurrentDictionary<Type, Func<IDomainEvent, INotification>> Factories =
        new();

    private readonly IPublisher _publisher;

    /// <summary>
    /// Initializes the bus.
    /// </summary>
    /// <param name="publisher">MediatR publisher used to dispatch notifications.</param>
    public MediatREventBus(IPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default
    )
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var factory = Factories.GetOrAdd(domainEvent.GetType(), CreateFactory);
        return _publisher.Publish(factory(domainEvent), cancellationToken);
    }

    private static Func<IDomainEvent, INotification> CreateFactory(Type eventType)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
        return domainEvent =>
            (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
    }
}

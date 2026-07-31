using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Domain.Common;
using MediatR;

namespace EnterpriseFramework.Infrastructure.Events;

/// <summary>
/// In-process implementation of <see cref="IEventBus"/> backed by MediatR
/// notifications.
///
/// Each published event is wrapped in
/// <see cref="Application.Abstractions.DomainEventNotification{TEvent}"/>
/// and delivered synchronously to every registered handler. Swapping this class
/// for a distributed transport (broker, Redis, SignalR fan-out) requires no
/// change in the modules that publish events.
/// </summary>
public sealed class MediatREventBus : IEventBus
{
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
        where TEvent : IDomainEvent =>
        _publisher.Publish(new DomainEventNotification<TEvent>(domainEvent), cancellationToken);
}

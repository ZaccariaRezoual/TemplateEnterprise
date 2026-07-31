using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Application-facing contract for publishing domain events.
///
/// Modules publish events without knowing the subscribers (event-driven rule);
/// the concrete dispatcher lives in Infrastructure so the transport (in-process
/// MediatR today, message broker or SignalR fan-out tomorrow) can be replaced
/// without touching application code.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a domain event to every registered subscriber.
    /// </summary>
    /// <typeparam name="TEvent">Concrete event type.</typeparam>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when all subscribers have handled the event.</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

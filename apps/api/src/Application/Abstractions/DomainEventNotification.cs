using EnterpriseFramework.Domain.Common;
using MediatR;

namespace EnterpriseFramework.Application.Abstractions;

/// <summary>
/// Adapter that wraps a domain event into a MediatR <see cref="INotification"/>.
///
/// Lives in Application (not Infrastructure) so modules can SUBSCRIBE to events
/// by implementing <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>
/// without referencing Infrastructure, and the Domain stays free of MediatR
/// (dependency rule).
/// </summary>
/// <typeparam name="TEvent">Concrete domain event type.</typeparam>
public sealed class DomainEventNotification<TEvent> : INotification
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Initializes the adapter around the event being published.
    /// </summary>
    /// <param name="domainEvent">The wrapped domain event.</param>
    public DomainEventNotification(TEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    /// <summary>The wrapped domain event.</summary>
    public TEvent DomainEvent { get; }
}

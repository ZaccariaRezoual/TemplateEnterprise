namespace EnterpriseFramework.Domain.Common;

/// <summary>
/// Marker contract for domain events.
///
/// A domain event describes something that already happened in the domain
/// (e.g. <c>UserCreated</c>) and is published through the event bus so other
/// modules can react without the publisher knowing them (event-driven rule).
///
/// Implement it with an immutable <c>record</c>. The domain layer only defines
/// the contract: dispatching is implemented in Infrastructure.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC instant at which the event occurred.</summary>
    DateTime OccurredOnUtc { get; }
}

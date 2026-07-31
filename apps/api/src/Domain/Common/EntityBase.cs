namespace EnterpriseFramework.Domain.Common;

/// <summary>
/// Base class for every domain entity.
///
/// Responsibilities:
/// - Owns the strongly-typed identifier.
/// - Collects domain events raised by the entity so Infrastructure can
///   dispatch them after a successful persistence operation.
///
/// It contains no persistence concerns: EF Core configuration lives in
/// Infrastructure (dependency rule).
/// </summary>
/// <typeparam name="TId">Type of the entity identifier (e.g. <see cref="Guid"/>).</typeparam>
public abstract class EntityBase<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Unique identifier of the entity.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Domain events raised by this entity and not yet dispatched.
    /// Read by the Infrastructure layer after persistence; never dispatch manually.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event to be dispatched after the entity is persisted.
    /// Call it from domain behavior methods, never from outside the entity.
    /// </summary>
    /// <param name="domainEvent">The event that occurred.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears the pending events. Called by Infrastructure once events
    /// have been dispatched; do not call from domain code.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

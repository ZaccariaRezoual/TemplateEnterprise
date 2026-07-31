using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Demo.Features.Echo;

/// <summary>
/// Domain event published whenever a text is echoed through the demo module.
/// Exists to prove the event bus end-to-end; real modules define events the
/// same way (immutable record implementing <see cref="IDomainEvent"/>).
/// </summary>
/// <param name="Text">The text that was echoed.</param>
public sealed record DemoEchoedEvent(string Text) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

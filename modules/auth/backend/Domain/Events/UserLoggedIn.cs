using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Auth.Domain.Events;

/// <summary>
/// Raised on every successful login. The Audit module (Fase 5) subscribes to
/// build the access trail; publishing here keeps Auth unaware of auditing.
/// </summary>
/// <param name="UserId">Account that signed in.</param>
public sealed record UserLoggedIn(Guid UserId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

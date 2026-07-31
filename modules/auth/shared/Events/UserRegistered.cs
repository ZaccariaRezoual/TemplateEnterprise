using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Auth.Contracts.Events;

/// <summary>
/// Published when a new account is created.
///
/// PUBLIC CONTRACT: other modules subscribe to this type, so changing it is a
/// breaking change for them. Add fields, never remove or repurpose them.
/// Known subscribers today: Authorization (grants the default role), Users
/// (creates the profile projection), Audit (records the event).
/// </summary>
/// <param name="UserId">Identifier of the new account.</param>
/// <param name="Email">Email of the new account.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
public sealed record UserRegistered(Guid UserId, string Email, string DisplayName) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

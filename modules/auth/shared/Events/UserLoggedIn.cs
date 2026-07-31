using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Auth.Contracts.Events;

/// <summary>
/// Published on every successful login.
///
/// PUBLIC CONTRACT: see <see cref="UserRegistered"/>. The Audit module
/// subscribes to build the access trail.
/// </summary>
/// <param name="UserId">Account that signed in.</param>
public sealed record UserLoggedIn(Guid UserId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Auth.Domain.Events;

/// <summary>
/// Raised when a new account is created. Other modules subscribe to react
/// (welcome email, audit trail, default settings) without the Auth module
/// knowing they exist.
/// </summary>
/// <param name="UserId">Identifier of the new account.</param>
/// <param name="Email">Email of the new account.</param>
public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Auth.Contracts.Events;

/// <summary>
/// Published when the bootstrap administrator account is created at startup.
///
/// It exists because a fresh installation would otherwise have no way in:
/// registration grants only the default role, and promoting an account
/// requires a permission nobody holds yet. Auth can create the ACCOUNT, but it
/// deliberately knows nothing about roles — so it announces the account and
/// lets Authorization decide what "administrator" means.
///
/// PUBLIC CONTRACT: other modules subscribe to this type, so changing it is a
/// breaking change for them. Add fields, never remove or repurpose them.
/// Known subscribers today: Authorization (grants the Admin role).
///
/// Raised only for a newly created account: a restart must not silently
/// re-promote an account an administrator deliberately demoted.
/// </summary>
/// <param name="UserId">Identifier of the account that was created.</param>
/// <param name="Email">Email of the account.</param>
public sealed record BootstrapAdminSeeded(Guid UserId, string Email) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

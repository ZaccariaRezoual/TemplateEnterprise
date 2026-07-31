using EnterpriseFramework.Modules.Auth.Domain;

namespace EnterpriseFramework.Modules.Auth.Contracts;

/// <summary>
/// Account identity exposed to clients. Entities never cross the API
/// boundary; this DTO is what OpenAPI (and therefore the SDK) sees.
///
/// It carries no roles or permissions on purpose: those belong to the
/// Authorization module and are served by <c>GET /api/authorization/me</c>.
/// Returning them here would create a second source of truth that silently
/// goes stale.
/// </summary>
/// <param name="Id">Account identifier.</param>
/// <param name="Email">Email address.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
public sealed record UserDto(Guid Id, string Email, string DisplayName)
{
    /// <summary>
    /// Maps a <see cref="User"/> entity to its client representation.
    /// </summary>
    /// <param name="user">The entity to map.</param>
    /// <returns>The DTO.</returns>
    public static UserDto FromUser(User user) => new(user.Id, user.Email, user.DisplayName);
}

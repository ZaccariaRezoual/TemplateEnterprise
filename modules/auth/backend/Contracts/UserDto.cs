using EnterpriseFramework.Modules.Auth.Domain;

namespace EnterpriseFramework.Modules.Auth.Contracts;

/// <summary>
/// Account information exposed to clients. Entities never cross the API
/// boundary; this DTO is what OpenAPI (and therefore the SDK) sees.
/// </summary>
/// <param name="Id">Account identifier.</param>
/// <param name="Email">Email address.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
/// <param name="Roles">Role names granted to the account.</param>
public sealed record UserDto(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles)
{
    /// <summary>
    /// Maps a <see cref="User"/> entity to its client representation.
    /// </summary>
    /// <param name="user">The entity to map.</param>
    /// <returns>The DTO.</returns>
    public static UserDto FromUser(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Roles);
}

using EnterpriseFramework.Modules.Users.Domain;

namespace EnterpriseFramework.Modules.Users.Contracts;

/// <summary>
/// User profile as exposed to clients.
/// </summary>
/// <param name="Id">Account identifier.</param>
/// <param name="Email">Email address.</param>
/// <param name="DisplayName">Name shown in the UI.</param>
/// <param name="JobTitle">Optional job title.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="CreatedAtUtc">When the account was created.</param>
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? JobTitle,
    bool IsActive,
    DateTime CreatedAtUtc
)
{
    /// <summary>
    /// Maps a <see cref="UserProfile"/> entity to its client representation.
    /// </summary>
    /// <param name="profile">The entity to map.</param>
    /// <returns>The DTO.</returns>
    public static UserProfileDto FromProfile(UserProfile profile) =>
        new(
            profile.Id,
            profile.Email,
            profile.DisplayName,
            profile.JobTitle,
            profile.IsActive,
            profile.CreatedAtUtc
        );
}

/// <summary>
/// A page of results.
/// </summary>
/// <typeparam name="TItem">Type of the items.</typeparam>
/// <param name="Items">Items of the current page.</param>
/// <param name="Page">1-based page index.</param>
/// <param name="PageSize">Maximum items per page.</param>
/// <param name="TotalCount">Total items across all pages.</param>
public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    int TotalCount
);

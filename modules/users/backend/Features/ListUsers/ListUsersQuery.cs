using EnterpriseFramework.Modules.Users.Contracts;
using EnterpriseFramework.Modules.Users.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Users.Features.ListUsers;

/// <summary>
/// Lists user profiles, paged and optionally filtered by a search term.
/// </summary>
/// <param name="Page">1-based page index.</param>
/// <param name="PageSize">Items per page (capped by the validator).</param>
/// <param name="Search">Case-insensitive match on email or display name.</param>
public sealed record ListUsersQuery(int Page = 1, int PageSize = 25, string? Search = null)
    : IRequest<PagedResult<UserProfileDto>>;

/// <summary>
/// Validation rules for <see cref="ListUsersQuery"/>. The page-size cap is a
/// protection, not a preference: an uncapped page size is a denial-of-service
/// vector on any list endpoint.
/// </summary>
public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    /// <summary>Initializes the rules.</summary>
    public ListUsersQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).GreaterThan(0).LessThanOrEqualTo(200);
        RuleFor(q => q.Search).MaximumLength(200);
    }
}

/// <summary>
/// Handles <see cref="ListUsersQuery"/> against this module's projection.
/// </summary>
public sealed class ListUsersQueryHandler
    : IRequestHandler<ListUsersQuery, PagedResult<UserProfileDto>>
{
    private readonly UsersDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Users persistence.</param>
    public ListUsersQueryHandler(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the requested page of profiles.
    /// </summary>
    /// <param name="request">The validated query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The page, with the total count for the pager.</returns>
    public async Task<PagedResult<UserProfileDto>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.Profiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Email, term) || EF.Functions.ILike(p.DisplayName, term)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderBy(p => p.DisplayName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<UserProfileDto>(
            [.. items.Select(UserProfileDto.FromProfile)],
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}

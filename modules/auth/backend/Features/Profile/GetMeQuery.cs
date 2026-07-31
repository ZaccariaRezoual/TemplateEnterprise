using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Auth.Contracts;
using EnterpriseFramework.Modules.Auth.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Auth.Features.Profile;

/// <summary>
/// Returns the profile of the authenticated caller. Used by the frontend to
/// restore the session UI after a page reload.
/// </summary>
public sealed record GetMeQuery : IRequest<UserDto>;

/// <summary>
/// Handles <see cref="GetMeQuery"/> resolving the caller via
/// <see cref="ICurrentUser"/> (never by trusting a client-provided id).
/// </summary>
public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
{
    private readonly AuthDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Auth persistence.</param>
    /// <param name="currentUser">Identity of the caller from the validated JWT.</param>
    public GetMeQueryHandler(AuthDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Loads the caller's profile.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The caller's profile.</returns>
    /// <exception cref="UnauthorizedException">
    /// Thrown when the token's account no longer exists (deleted user with a
    /// still-valid access token).
    /// </exception>
    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId =
            _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var user = await _dbContext
            .Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        return user is null
            ? throw new UnauthorizedException("This account no longer exists.")
            : UserDto.FromUser(user);
    }
}

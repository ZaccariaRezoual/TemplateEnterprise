using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Authorization.Contracts;
using EnterpriseFramework.Modules.Authorization.Services;
using MediatR;

namespace EnterpriseFramework.Modules.Authorization.Features.Roles;

/// <summary>
/// Returns the caller's effective roles and permissions.
///
/// The frontend calls it after signing in to drive the UI (hide what the user
/// cannot do). It is a CONVENIENCE, never a security boundary: the API
/// enforces permissions on every endpoint regardless of what the UI shows.
/// </summary>
public sealed record GetMyAuthorizationQuery : IRequest<UserAuthorizationDto>;

/// <summary>
/// Handles <see cref="GetMyAuthorizationQuery"/> from the authenticated
/// caller's identity, never from a client-supplied id.
/// </summary>
public sealed class GetMyAuthorizationQueryHandler
    : IRequestHandler<GetMyAuthorizationQuery, UserAuthorizationDto>
{
    private readonly PermissionReader _permissionReader;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="permissionReader">Resolves effective roles and permissions.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    public GetMyAuthorizationQueryHandler(
        PermissionReader permissionReader,
        ICurrentUser currentUser
    )
    {
        _permissionReader = permissionReader;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Loads the caller's authorization.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The caller's roles and permissions.</returns>
    /// <exception cref="UnauthorizedException">Thrown when the caller is anonymous.</exception>
    public async Task<UserAuthorizationDto> Handle(
        GetMyAuthorizationQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId =
            _currentUser.UserId ?? throw new UnauthorizedException("Authentication is required.");

        var (roles, permissions) = await _permissionReader
            .GetForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return new UserAuthorizationDto(roles, permissions);
    }
}

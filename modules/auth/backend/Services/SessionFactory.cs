using EnterpriseFramework.Modules.Auth.Contracts;
using EnterpriseFramework.Modules.Auth.Domain;
using EnterpriseFramework.Modules.Auth.Persistence;

namespace EnterpriseFramework.Modules.Auth.Services;

/// <summary>
/// Builds a complete session for an authenticated account: signed access
/// token plus a persisted, rotated-in refresh token.
///
/// Shared by the register, login and refresh handlers so session shape and
/// refresh-token bookkeeping exist in exactly one place. Does NOT save: the
/// owning handler commits the unit of work.
/// </summary>
public sealed class SessionFactory
{
    private readonly TokenService _tokenService;
    private readonly AuthDbContext _dbContext;

    /// <summary>
    /// Initializes the factory.
    /// </summary>
    /// <param name="tokenService">Issues access and refresh tokens.</param>
    /// <param name="dbContext">Auth persistence, used to store the refresh token hash.</param>
    public SessionFactory(TokenService tokenService, AuthDbContext dbContext)
    {
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a session for the given account and stages the refresh token.
    /// </summary>
    /// <param name="user">The authenticated account.</param>
    /// <returns>The session, including the raw refresh token for the cookie.</returns>
    public AuthSession Create(User user)
    {
        var (accessToken, expiresAtUtc) = _tokenService.CreateAccessToken(user);
        var (rawRefreshToken, refreshTokenHash) = TokenService.CreateRefreshToken();

        _dbContext.RefreshTokens.Add(
            RefreshToken.Issue(user.Id, refreshTokenHash, _tokenService.RefreshTokenLifetime)
        );

        return new AuthSession(
            new AuthResponse(accessToken, expiresAtUtc, UserDto.FromUser(user)),
            rawRefreshToken
        );
    }
}

using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Auth.Contracts;
using EnterpriseFramework.Modules.Auth.Persistence;
using EnterpriseFramework.Modules.Auth.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Auth.Features.Refresh;

/// <summary>
/// Handles <see cref="RefreshCommand"/> implementing rotation with reuse
/// detection:
///
/// - Valid, active token → revoke it, record its successor, issue a new pair.
/// - Token already rotated or revoked → someone is replaying an old token
///   (theft indicator): EVERY active token of that account is revoked and the
///   caller gets 401, forcing a fresh interactive login everywhere.
/// </summary>
public sealed partial class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthSession>
{
    private readonly AuthDbContext _dbContext;
    private readonly SessionFactory _sessionFactory;
    private readonly ILogger<RefreshCommandHandler> _logger;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; all sessions revoked"
    )]
    private static partial void LogReuseDetected(ILogger logger, Guid userId);

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Auth persistence.</param>
    /// <param name="sessionFactory">Builds the replacement session.</param>
    /// <param name="logger">Audit logging of reuse detection.</param>
    public RefreshCommandHandler(
        AuthDbContext dbContext,
        SessionFactory sessionFactory,
        ILogger<RefreshCommandHandler> logger
    )
    {
        _dbContext = dbContext;
        _sessionFactory = sessionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Rotates the refresh token and issues a new session.
    /// </summary>
    /// <param name="request">The command carrying the raw token.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The new session, including the new raw refresh token.</returns>
    /// <exception cref="UnauthorizedException">
    /// Thrown when the token is unknown, expired, or replayed after rotation.
    /// </exception>
    public async Task<AuthSession> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken
    )
    {
        var tokenHash = TokenService.HashRefreshToken(request.RawRefreshToken);
        var stored = await _dbContext
            .RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (stored is null)
        {
            throw new UnauthorizedException("The session is no longer valid. Sign in again.");
        }

        if (!stored.IsActive)
        {
            // Reuse of a rotated/expired token: revoke the whole family.
            LogReuseDetected(_logger, stored.UserId);
            var activeTokens = await _dbContext
                .RefreshTokens.Where(t => t.UserId == stored.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var token in activeTokens)
            {
                token.Revoke();
            }
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedException("The session is no longer valid. Sign in again.");
        }

        var user = await _dbContext
            .Users.SingleAsync(u => u.Id == stored.UserId, cancellationToken)
            .ConfigureAwait(false);

        var session = _sessionFactory.Create(user);
        stored.Revoke(TokenService.HashRefreshToken(session.RawRefreshToken));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }
}

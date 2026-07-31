using EnterpriseFramework.Modules.Auth.Persistence;
using EnterpriseFramework.Modules.Auth.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Auth.Features.Logout;

/// <summary>
/// Ends the current session by revoking its refresh token. Idempotent: an
/// unknown or already-revoked token is not an error, because the outcome the
/// caller asked for — "this session cannot be used again" — already holds.
/// </summary>
/// <param name="RawRefreshToken">Raw refresh token from the cookie; may be absent.</param>
public sealed record LogoutCommand(string? RawRefreshToken) : IRequest;

/// <summary>
/// Handles <see cref="LogoutCommand"/>.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly AuthDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Auth persistence.</param>
    public LogoutCommandHandler(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Revokes the presented refresh token, when it exists and is active.
    /// </summary>
    /// <param name="request">The command carrying the raw token, if any.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RawRefreshToken))
        {
            return;
        }

        var tokenHash = TokenService.HashRefreshToken(request.RawRefreshToken);
        var stored = await _dbContext
            .RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (stored is { RevokedAtUtc: null })
        {
            stored.Revoke();
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

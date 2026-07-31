using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Auth.Contracts;
using EnterpriseFramework.Modules.Auth.Domain.Events;
using EnterpriseFramework.Modules.Auth.Persistence;
using EnterpriseFramework.Modules.Auth.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Auth.Features.Login;

/// <summary>
/// Handles <see cref="LoginCommand"/>.
///
/// Security behavior:
/// - Unknown email and wrong password produce the SAME error, so the endpoint
///   cannot be used to enumerate accounts.
/// - Failed attempts are logged with the email (audit trail); successful ones
///   publish <c>UserLoggedIn</c> for the Audit module.
/// </summary>
public sealed partial class LoginCommandHandler : IRequestHandler<LoginCommand, AuthSession>
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionFactory _sessionFactory;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LoginCommandHandler> _logger;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed login attempt for {Email}")]
    private static partial void LogFailedLogin(ILogger logger, string email);

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Auth persistence.</param>
    /// <param name="passwordHasher">Verifies the password.</param>
    /// <param name="sessionFactory">Builds the session on success.</param>
    /// <param name="eventBus">Publishes the login event.</param>
    /// <param name="logger">Audit logging of failed attempts.</param>
    public LoginCommandHandler(
        AuthDbContext dbContext,
        IPasswordHasher passwordHasher,
        SessionFactory sessionFactory,
        IEventBus eventBus,
        ILogger<LoginCommandHandler> logger
    )
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _sessionFactory = sessionFactory;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the credentials and creates a session.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The session, including the raw refresh token for the cookie.</returns>
    /// <exception cref="UnauthorizedException">
    /// Thrown for unknown email OR wrong password — identical on purpose.
    /// </exception>
    public async Task<AuthSession> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Email.ToUpperInvariant();
        var user = await _dbContext
            .Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            LogFailedLogin(_logger, request.Email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        var session = _sessionFactory.Create(user);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _eventBus
            .PublishAsync(new UserLoggedIn(user.Id), cancellationToken)
            .ConfigureAwait(false);

        return session;
    }
}

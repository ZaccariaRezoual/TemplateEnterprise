using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Auth.Contracts;
using EnterpriseFramework.Modules.Auth.Domain;
using EnterpriseFramework.Modules.Auth.Persistence;
using EnterpriseFramework.Modules.Auth.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Auth.Features.Register;

/// <summary>
/// Handles <see cref="RegisterCommand"/>: creates the account, publishes
/// <c>UserRegistered</c> on the event bus and returns a ready session, so a
/// new user is signed in without a second round-trip.
/// </summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthSession>
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionFactory _sessionFactory;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Auth persistence.</param>
    /// <param name="passwordHasher">Hashes the raw password.</param>
    /// <param name="sessionFactory">Builds the session for the new account.</param>
    /// <param name="eventBus">Publishes the registration event.</param>
    public RegisterCommandHandler(
        AuthDbContext dbContext,
        IPasswordHasher passwordHasher,
        SessionFactory sessionFactory,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _sessionFactory = sessionFactory;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Creates the account and its first session.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The session, including the raw refresh token for the cookie.</returns>
    /// <exception cref="BusinessException">Thrown when the email is already registered.</exception>
    public async Task<AuthSession> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken
    )
    {
        var normalized = request.Email.ToUpperInvariant();
        var emailTaken = await _dbContext
            .Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken)
            .ConfigureAwait(false);
        if (emailTaken)
        {
            throw new BusinessException("This email address is already registered.");
        }

        var user = User.Register(
            request.Email,
            request.DisplayName,
            _passwordHasher.Hash(request.Password)
        );
        _dbContext.Users.Add(user);
        var session = _sessionFactory.Create(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Two registrations raced past the AnyAsync check; the unique
            // index on NormalizedEmail is the real guarantee.
            throw new BusinessException("This email address is already registered.");
        }

        foreach (var domainEvent in user.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
        user.ClearDomainEvents();

        return session;
    }
}

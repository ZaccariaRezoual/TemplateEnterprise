using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Audit.Domain;
using EnterpriseFramework.Modules.Audit.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EnterpriseFramework.Modules.Audit.Behaviors;

/// <summary>
/// Records an audit entry for every COMMAND that flows through the MediatR
/// pipeline — including the ones that fail.
///
/// Automatic by construction: a new module gets an audit trail without
/// writing a line of auditing code, and a developer cannot forget to audit a
/// sensitive action. Queries are skipped: recording every read would bury the
/// actions that matter in noise.
///
/// Only the command NAME is stored, never its payload: commands carry
/// passwords and personal data, and an audit table is exactly the wrong place
/// for them. Payload capture, if ever needed, must be opt-in per command.
///
/// A failure to write the audit entry never fails the request: losing an
/// audit row is bad, refusing a legitimate operation because of it is worse.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">Handler response type.</typeparam>
public sealed partial class AuditBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly AuditDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not write audit entry for {Action}")]
    private static partial void LogAuditFailure(ILogger logger, string action, Exception exception);

    /// <summary>
    /// Initializes the behavior.
    /// </summary>
    /// <param name="dbContext">Audit persistence.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    /// <param name="httpContextAccessor">Provides the request correlation id.</param>
    /// <param name="logger">Reports audit write failures.</param>
    public AuditBehavior(
        AuditDbContext dbContext,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditBehavior<TRequest, TResponse>> logger
    )
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Runs the handler and records the outcome.
    /// </summary>
    /// <param name="request">Incoming request.</param>
    /// <param name="next">Continuation to the handler.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The handler response.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var action = typeof(TRequest).Name;

        if (!action.EndsWith("Command", StringComparison.Ordinal))
        {
            return await next().ConfigureAwait(false);
        }

        try
        {
            var response = await next().ConfigureAwait(false);
            await RecordAsync(action, succeeded: true, cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            // Record the attempt, then let the original failure propagate:
            // failed attempts are often the interesting ones.
            await RecordAsync(action, succeeded: false, CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    private async Task RecordAsync(string action, bool succeeded, CancellationToken cancellationToken)
    {
        try
        {
            var correlationId =
                _httpContextAccessor.HttpContext?.Items["X-Correlation-ID"] as string;

            _dbContext.Entries.Add(
                AuditEntry.Record(
                    _currentUser.UserId,
                    action,
                    AuditSource.Command,
                    succeeded,
                    correlationId
                )
            );
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogAuditFailure(_logger, action, exception);
        }
    }
}

using EnterpriseFramework.Domain.Common;

namespace EnterpriseFramework.Modules.Audit.Domain;

/// <summary>
/// One recorded action.
///
/// Append-only by design: entries are never updated or deleted through the
/// application, because an audit trail that can be rewritten is not evidence.
/// Retention is an operational concern (partitioning or archival), not an
/// application feature.
/// </summary>
public sealed class AuditEntry : EntityBase<Guid>
{
    private AuditEntry() { }

    /// <summary>Account that performed the action, or null when anonymous.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// What happened, as a stable name: the command type for pipeline
    /// entries ("SetUserRolesCommand"), the event type for event entries.
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>How the entry was produced (command or event).</summary>
    public AuditSource Source { get; private set; }

    /// <summary>Whether the audited operation succeeded.</summary>
    public bool Succeeded { get; private set; }

    /// <summary>Correlation id of the originating request, to join with logs.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>UTC instant the action occurred.</summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>
    /// Records an action.
    /// </summary>
    /// <param name="userId">Account that acted, or null when anonymous.</param>
    /// <param name="action">Stable action name.</param>
    /// <param name="source">How the entry was produced.</param>
    /// <param name="succeeded">Whether the operation succeeded.</param>
    /// <param name="correlationId">Correlation id of the request.</param>
    /// <returns>The entry to persist.</returns>
    public static AuditEntry Record(
        Guid? userId,
        string action,
        AuditSource source,
        bool succeeded,
        string? correlationId
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Source = source,
            Succeeded = succeeded,
            CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow,
        };
}

/// <summary>Origin of an audit entry.</summary>
public enum AuditSource
{
    /// <summary>Recorded automatically from a command in the MediatR pipeline.</summary>
    Command = 0,

    /// <summary>Recorded from a domain event published by another module.</summary>
    Event = 1,
}

using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Settings.Domain;
using EnterpriseFramework.Modules.Settings.Persistence;
using EnterpriseFramework.Modules.Settings.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFramework.Modules.Settings.Features;

/// <summary>
/// One effective setting as seen by the caller.
/// </summary>
/// <param name="Key">Setting key.</param>
/// <param name="Value">Effective value after layering.</param>
/// <param name="Description">What the setting controls.</param>
/// <param name="Scope">Where it may be overridden.</param>
/// <param name="ValueType">Declared type of the value.</param>
/// <param name="IsOverriddenByUser">Whether the caller has their own value.</param>
public sealed record SettingDto(
    string Key,
    string Value,
    string Description,
    string Scope,
    string ValueType,
    bool IsOverriddenByUser
);

/// <summary>
/// Returns every declared setting with its effective value for the caller.
/// One call is enough to configure a whole UI, instead of one request per key.
/// </summary>
public sealed record GetEffectiveSettingsQuery : IRequest<IReadOnlyList<SettingDto>>;

/// <summary>
/// Handles <see cref="GetEffectiveSettingsQuery"/>.
/// </summary>
public sealed class GetEffectiveSettingsQueryHandler
    : IRequestHandler<GetEffectiveSettingsQuery, IReadOnlyList<SettingDto>>
{
    private readonly SettingsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Settings persistence.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    public GetEffectiveSettingsQueryHandler(SettingsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Resolves every declared setting in a single query.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Effective settings for the caller.</returns>
    public async Task<IReadOnlyList<SettingDto>> Handle(
        GetEffectiveSettingsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = _currentUser.UserId;

        // One round-trip for all keys: resolving per key would issue a query
        // per setting on every page load.
        var stored = await _dbContext
            .Settings.AsNoTracking()
            .Where(s => s.UserId == null || s.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. SettingKeys.All.Select(definition =>
            {
                var userValue = stored
                    .FirstOrDefault(s => s.Key == definition.Key && s.UserId == userId)
                    ?.Value;
                var globalValue = stored
                    .FirstOrDefault(s => s.Key == definition.Key && s.UserId == null)
                    ?.Value;

                return new SettingDto(
                    definition.Key,
                    userValue ?? globalValue ?? definition.DefaultValue,
                    definition.Description,
                    definition.Scope.ToString(),
                    definition.ValueType.ToString(),
                    userValue is not null
                );
            }),
        ];
    }
}

/// <summary>
/// Sets a setting value.
/// </summary>
/// <param name="Key">Setting key; must be declared.</param>
/// <param name="Value">Raw value; must parse as the declared type.</param>
/// <param name="ForCurrentUser">
/// Whether the value is the caller's own override (true) or the global value.
/// </param>
public sealed record SetSettingCommand(string Key, string Value, bool ForCurrentUser) : IRequest;

/// <summary>
/// Handles <see cref="SetSettingCommand"/>.
/// </summary>
public sealed class SetSettingCommandHandler : IRequestHandler<SetSettingCommand>
{
    private readonly SettingsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Settings persistence.</param>
    /// <param name="currentUser">Identity of the caller.</param>
    public SetSettingCommandHandler(SettingsDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Stores the value, creating or updating the row for its scope.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotFoundException">Thrown when the key is not declared.</exception>
    /// <exception cref="BusinessException">
    /// Thrown when the value does not match the declared type, or when a
    /// global-only setting is set per user.
    /// </exception>
    /// <exception cref="UnauthorizedException">Thrown when a user override has no caller.</exception>
    public async Task Handle(SetSettingCommand request, CancellationToken cancellationToken)
    {
        var definition =
            SettingKeys.Find(request.Key) ?? throw new NotFoundException("Setting", request.Key);

        if (!SettingsReader.IsValid(definition, request.Value))
        {
            throw new BusinessException(
                $"'{request.Value}' is not a valid {definition.ValueType} value for '{definition.Key}'."
            );
        }

        Guid? userId = null;
        if (request.ForCurrentUser)
        {
            if (definition.Scope != SettingScope.User)
            {
                throw new BusinessException($"'{definition.Key}' cannot be set per user.");
            }
            userId =
                _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication is required.");
        }

        var existing = await _dbContext
            .Settings.SingleOrDefaultAsync(
                s => s.Key == request.Key && s.UserId == userId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.Settings.Add(Setting.Create(request.Key, request.Value, userId));
        }
        else
        {
            existing.Update(request.Value);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

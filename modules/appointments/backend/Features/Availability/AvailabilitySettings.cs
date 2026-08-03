using System.Globalization;
using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Availability;

/// <summary>Reads the whole availability configuration.</summary>
public sealed record GetAvailabilitySettingsQuery : IRequest<AvailabilitySettingsDto>;

/// <summary>
/// Handles <see cref="GetAvailabilitySettingsQuery"/>.
/// </summary>
public sealed class GetAvailabilitySettingsQueryHandler
    : IRequestHandler<GetAvailabilitySettingsQuery, AvailabilitySettingsDto>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration.</param>
    public GetAvailabilitySettingsQueryHandler(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    /// <summary>
    /// Returns the weekly schedule and its exceptions.
    /// </summary>
    /// <param name="request">The query; it carries no parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The configuration.</returns>
    public async Task<AvailabilitySettingsDto> Handle(
        GetAvailabilitySettingsQuery request,
        CancellationToken cancellationToken
    )
    {
        var rules = await _dbContext
            .Rules.AsNoTracking()
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartLocal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exceptions = await _dbContext
            .Exceptions.AsNoTracking()
            .OrderBy(exception => exception.DateLocal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AvailabilitySettingsDto(
            _options.ResolveTimeZone().Id,
            [
                .. rules.Select(rule => new AvailabilityRuleDto(
                    rule.DayOfWeek,
                    Format(rule.StartLocal),
                    Format(rule.EndLocal)
                )),
            ],
            [
                .. exceptions.Select(exception => new AvailabilityOverrideDto(
                    exception.DateLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    exception.IsClosed,
                    exception.StartLocal is null ? null : Format(exception.StartLocal.Value),
                    exception.EndLocal is null ? null : Format(exception.EndLocal.Value),
                    exception.Reason
                )),
            ]
        );
    }

    internal static string Format(TimeOnly value) =>
        value.ToString("HH:mm", CultureInfo.InvariantCulture);
}

/// <summary>
/// Replaces the whole availability configuration.
///
/// A wholesale replace rather than per-row endpoints, because that is how a
/// weekly schedule is thought about and edited: "Tuesdays move to the
/// afternoon" is one decision, and splitting it into three requests invites
/// the state where the first two applied and the third did not.
/// </summary>
/// <param name="Rules">The complete weekly schedule.</param>
/// <param name="Exceptions">The complete list of closures and extra openings.</param>
public sealed record SaveAvailabilitySettingsCommand(
    IReadOnlyList<AvailabilityRuleDto> Rules,
    IReadOnlyList<AvailabilityOverrideDto> Exceptions
) : IRequest<AvailabilitySettingsDto>;

/// <summary>
/// Validation rules for <see cref="SaveAvailabilitySettingsCommand"/>.
/// </summary>
public sealed class SaveAvailabilitySettingsCommandValidator
    : AbstractValidator<SaveAvailabilitySettingsCommand>
{
    /// <summary>Initializes the rules.</summary>
    public SaveAvailabilitySettingsCommandValidator()
    {
        RuleForEach(command => command.Rules)
            .ChildRules(rule =>
            {
                rule.RuleFor(entry => entry.StartLocal).Must(BeATime).WithMessage("Use HH:mm.");
                rule.RuleFor(entry => entry.EndLocal).Must(BeATime).WithMessage("Use HH:mm.");
                rule.RuleFor(entry => entry)
                    .Must(entry => Parse(entry.EndLocal) > Parse(entry.StartLocal))
                    .When(entry => BeATime(entry.StartLocal) && BeATime(entry.EndLocal))
                    .WithMessage("The closing time has to be after the opening one.");
            });

        RuleForEach(command => command.Exceptions)
            .ChildRules(exception =>
            {
                exception
                    .RuleFor(entry => entry.DateLocal)
                    .Must(BeADate)
                    .WithMessage("Use yyyy-MM-dd.");
                exception.RuleFor(entry => entry.Reason).NotEmpty().MaximumLength(200);
                exception
                    .RuleFor(entry => entry)
                    .Must(entry => entry.StartLocal is not null && entry.EndLocal is not null)
                    .When(entry => !entry.IsClosed)
                    .WithMessage("An extraordinary opening needs both a start and an end.");
            });
    }

    private static bool BeATime(string? value) =>
        value is not null
        && TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool BeADate(string? value) =>
        value is not null
        && DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static TimeOnly Parse(string value) =>
        TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);
}

/// <summary>
/// Handles <see cref="SaveAvailabilitySettingsCommand"/>.
/// </summary>
public sealed class SaveAvailabilitySettingsCommandHandler
    : IRequestHandler<SaveAvailabilitySettingsCommand, AvailabilitySettingsDto>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="sender">Used to read back the saved configuration.</param>
    public SaveAvailabilitySettingsCommandHandler(AppointmentsDbContext dbContext, ISender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    /// <summary>
    /// Replaces the configuration.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The saved configuration, read back.</returns>
    public async Task<AvailabilitySettingsDto> Handle(
        SaveAvailabilitySettingsCommand request,
        CancellationToken cancellationToken
    )
    {
        // Replace, not merge. The client sent the whole document, so anything
        // absent from it was deleted on purpose — merging would make removing
        // an opening impossible.
        _dbContext.Rules.RemoveRange(_dbContext.Rules);
        _dbContext.Exceptions.RemoveRange(_dbContext.Exceptions);

        foreach (var rule in request.Rules)
        {
            _dbContext.Rules.Add(
                AvailabilityRule.Create(rule.DayOfWeek, ParseTime(rule.StartLocal), ParseTime(rule.EndLocal))
            );
        }

        foreach (var exception in request.Exceptions)
        {
            var date = ParseDate(exception.DateLocal);

            _dbContext.Exceptions.Add(
                exception.IsClosed
                    ? AvailabilityOverride.Closure(date, exception.Reason)
                    : AvailabilityOverride.Opening(
                        date,
                        ParseTime(exception.StartLocal!),
                        ParseTime(exception.EndLocal!),
                        exception.Reason
                    )
            );
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Appointments already taken are NOT re-checked against the new
        // schedule, and that is deliberate: shrinking the opening hours must
        // not silently cancel bookings people already have. The calendar
        // shows them outside the hours, and a human decides.
        return await _sender.Send(new GetAvailabilitySettingsQuery(), cancellationToken)
            .ConfigureAwait(false);
    }

    private static TimeOnly ParseTime(string value) =>
        TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}

using System.Globalization;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Domain.Availability;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Availability;

/// <summary>
/// Asks when a service can be booked.
/// </summary>
/// <param name="ServiceId">Service to check.</param>
/// <param name="From">First local date, inclusive.</param>
/// <param name="To">Last local date, inclusive.</param>
public sealed record GetAvailabilityQuery(Guid ServiceId, DateOnly From, DateOnly To)
    : IRequest<AvailabilityDto>;

/// <summary>
/// Validation rules for <see cref="GetAvailabilityQuery"/>.
///
/// The range cap is a protection, not a preference: this endpoint is
/// ANONYMOUS, and an uncapped range lets anyone ask for ten years of slots
/// and make the server compute them.
/// </summary>
public sealed class GetAvailabilityQueryValidator : AbstractValidator<GetAvailabilityQuery>
{
    /// <summary>Longest range a single request may ask for.</summary>
    public const int MaxRangeDays = 62;

    /// <summary>Initializes the rules.</summary>
    public GetAvailabilityQueryValidator()
    {
        RuleFor(query => query.ServiceId).NotEmpty();
        RuleFor(query => query.To).GreaterThanOrEqualTo(query => query.From);
        RuleFor(query => query)
            .Must(query => query.To.DayNumber - query.From.DayNumber < MaxRangeDays)
            .WithMessage($"Ask for at most {MaxRangeDays} days at a time.");
    }
}

/// <summary>
/// Handles <see cref="GetAvailabilityQuery"/>.
///
/// Responsibilities: gather what the calculation needs — the service's
/// duration from this module's projection, the rules, the exceptions and the
/// confirmed appointments — and hand them to the pure calculator. It contains
/// no slot arithmetic itself, which is what keeps that arithmetic testable
/// without a database.
///
/// What it returns is deliberately thin: instants and nothing else. This
/// endpoint is anonymous, so it must never reveal WHO booked the hours that
/// are missing — that is the difference between a public calendar and a data
/// leak.
/// </summary>
public sealed class GetAvailabilityQueryHandler : IRequestHandler<GetAvailabilityQuery, AvailabilityDto>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration.</param>
    /// <param name="timeProvider">Clock, injected so tests can control it.</param>
    public GetAvailabilityQueryHandler(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Computes the bookable slots.
    /// </summary>
    /// <param name="request">The validated query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>One entry per day of the range.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the service is unknown to this module, or is not bookable.
    /// The same answer for both: a visitor must not be able to tell a service
    /// that does not exist from one that exists but is a draft.
    /// </exception>
    public async Task<AvailabilityDto> Handle(
        GetAvailabilityQuery request,
        CancellationToken cancellationToken
    )
    {
        var service =
            await _dbContext
                .Services.AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == request.ServiceId, cancellationToken)
                .ConfigureAwait(false) ?? throw new NotFoundException("Service", request.ServiceId);

        if (!service.CanBeBooked)
        {
            throw new NotFoundException("Service", request.ServiceId);
        }

        var timeZone = _options.ResolveTimeZone();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var rules = await _dbContext.Rules.AsNoTracking().ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exceptions = await _dbContext
            .Exceptions.AsNoTracking()
            .Where(exception =>
                exception.DateLocal >= request.From && exception.DateLocal <= request.To
            )
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // A window wide enough to catch a booking that starts the day before
        // the range and runs into it, plus the buffer on either side. Built
        // through UtcInstant: a DateTime with no Kind is what PostgreSQL
        // refuses to store in a timestamptz column.
        var windowStartUtc = Domain.UtcInstant.From(request.From.AddDays(-2), TimeOnly.MinValue);
        var windowEndUtc = Domain.UtcInstant.From(request.To.AddDays(2), TimeOnly.MinValue);

        var busy = await _dbContext
            .Appointments.AsNoTracking()
            .Where(appointment =>
                appointment.Status == Domain.AppointmentStatus.Confirmed
                && appointment.EndUtc > windowStartUtc
                && appointment.StartUtc < windowEndUtc
            )
            .Select(appointment => new { appointment.StartUtc, appointment.EndUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var days = AvailabilityCalculator.Calculate(
            request.From,
            request.To,
            rules,
            exceptions,
            [.. busy.Select(interval => new BusyInterval(interval.StartUtc, interval.EndUtc))],
            new AvailabilityRequest(
                timeZone,
                service.DurationMinutes!.Value,
                _options.SlotGranularityMinutes,
                _options.BufferMinutes,
                _options.MinimumNoticeHours,
                _options.MaxAdvanceDays,
                nowUtc
            )
        );

        return new AvailabilityDto(
            service.Id,
            service.Title,
            service.DurationMinutes.Value,
            timeZone.Id,
            [
                .. days.Select(day => new AvailableDayDto(
                    day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    [.. day.Slots.Select(slot => new SlotDto(slot.StartUtc, slot.EndUtc))]
                )),
            ]
        );
    }
}

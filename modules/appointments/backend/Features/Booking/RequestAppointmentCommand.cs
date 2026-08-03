using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Domain.Availability;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Booking;

/// <summary>
/// A customer asks for an appointment.
/// </summary>
/// <param name="ServiceId">Service being booked.</param>
/// <param name="StartUtc">Start instant chosen from the offered slots, UTC.</param>
/// <param name="ContactPhone">Number to reach the customer on for this booking.</param>
/// <param name="CustomerNote">Optional note.</param>
public sealed record RequestAppointmentCommand(
    Guid ServiceId,
    DateTime StartUtc,
    string ContactPhone,
    string? CustomerNote
) : IRequest<MyAppointmentDto>;

/// <summary>
/// Validation rules for <see cref="RequestAppointmentCommand"/>.
///
/// The phone is required here, in one place, rather than being "asked for" by
/// the form: the endpoint is the boundary, and a booking without a way to
/// reach the customer is the one that costs the business a slot when
/// something changes.
/// </summary>
public sealed class RequestAppointmentCommandValidator
    : AbstractValidator<RequestAppointmentCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RequestAppointmentCommandValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.StartUtc).NotEmpty();
        RuleFor(command => command.ContactPhone)
            .NotEmpty()
            .MaximumLength(40)
            .WithMessage("Leave a phone number: it is how we reach you if anything changes.");
        RuleFor(command => command.CustomerNote).MaximumLength(2000);
    }
}

/// <summary>
/// Handles <see cref="RequestAppointmentCommand"/>.
///
/// Responsibilities: check that the chosen instant is one the engine would
/// actually have offered, record the REQUEST, remember the phone number for
/// next time, and announce it.
///
/// It re-computes availability rather than trusting the client, and that is
/// not paranoia about the browser: the page was rendered minutes ago, and in
/// between the business may have closed the day, someone may have taken the
/// hour, or the minimum notice may have passed. The answer has to be the one
/// that is true now.
///
/// What it does NOT do is reserve the slot. A request occupies nothing —
/// several people may ask for the same hour and whoever administers chooses —
/// so this command can never hit the exclusion constraint.
/// </summary>
public sealed class RequestAppointmentCommandHandler
    : IRequestHandler<RequestAppointmentCommand, MyAppointmentDto>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration.</param>
    /// <param name="currentUser">The customer making the request.</param>
    /// <param name="eventBus">Bus the public events are published on.</param>
    /// <param name="timeProvider">Clock, injected so tests can control it.</param>
    public RequestAppointmentCommandHandler(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options,
        ICurrentUser currentUser,
        IEventBus eventBus,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _currentUser = currentUser;
        _eventBus = eventBus;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Records the request.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The appointment, as its owner sees it.</returns>
    /// <exception cref="UnauthorizedException">Thrown when nobody is signed in.</exception>
    /// <exception cref="NotFoundException">Thrown when the service cannot be booked.</exception>
    /// <exception cref="BusinessException">
    /// Thrown when the chosen instant is no longer offered — the case the
    /// booking page must handle by sending the visitor back to the hours.
    /// </exception>
    public async Task<MyAppointmentDto> Handle(
        RequestAppointmentCommand request,
        CancellationToken cancellationToken
    )
    {
        var customerId =
            _currentUser.UserId
            ?? throw new UnauthorizedException("Sign in to book an appointment.");

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
        var startUtc = UtcInstant.From(request.StartUtc);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, timeZone));

        var offered = await IsOfferedAsync(service, localDate, startUtc, timeZone, cancellationToken)
            .ConfigureAwait(false);

        if (!offered)
        {
            throw new BusinessException(
                "That time is no longer available. Please choose another one."
            );
        }

        var appointment = Appointment.Request(
            service.Id,
            customerId,
            startUtc,
            startUtc.AddMinutes(service.DurationMinutes!.Value),
            request.ContactPhone,
            request.CustomerNote
        );

        _dbContext.Appointments.Add(appointment);

        var customer = await _dbContext
            .Customers.SingleOrDefaultAsync(entity => entity.Id == customerId, cancellationToken)
            .ConfigureAwait(false);

        // Remembered so the next booking form arrives filled in. It is a
        // convenience, never a source of truth: the appointment carries its
        // own number.
        customer?.RememberPhone(request.ContactPhone);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _eventBus
            .PublishAsync(
                new AppointmentRequested(
                    Summarise(appointment, service, customer, timeZone),
                    appointment.CustomerNote
                ),
                cancellationToken
            )
            .ConfigureAwait(false);

        return AppointmentMapper.ToMine(appointment, service.Title, _options, _timeProvider);
    }

    /// <summary>
    /// Asks the availability engine whether this exact instant is on offer.
    ///
    /// One day is enough of a range: the slot the customer clicked is on the
    /// day they were looking at, and computing more would be work nobody
    /// reads.
    /// </summary>
    private async Task<bool> IsOfferedAsync(
        ServiceProjection service,
        DateOnly localDate,
        DateTime startUtc,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken
    )
    {
        var rules = await _dbContext.Rules.AsNoTracking().ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exceptions = await _dbContext
            .Exceptions.AsNoTracking()
            .Where(exception => exception.DateLocal == localDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var windowStartUtc = startUtc.AddDays(-2);
        var windowEndUtc = startUtc.AddDays(2);

        var busy = await _dbContext
            .Appointments.AsNoTracking()
            .Where(appointment =>
                appointment.Status == AppointmentStatus.Confirmed
                && appointment.EndUtc > windowStartUtc
                && appointment.StartUtc < windowEndUtc
            )
            .Select(appointment => new { appointment.StartUtc, appointment.EndUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var days = AvailabilityCalculator.Calculate(
            localDate,
            localDate,
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
                _timeProvider.GetUtcNow().UtcDateTime
            )
        );

        return days.Any(day => day.Slots.Any(slot => slot.StartUtc == startUtc));
    }

    /// <summary>Builds the summary every public event of this module carries.</summary>
    internal static AppointmentSummary Summarise(
        Appointment appointment,
        ServiceProjection service,
        CustomerProjection? customer,
        TimeZoneInfo timeZone
    ) =>
        new(
            appointment.Id,
            appointment.CustomerUserId,
            customer?.Email ?? string.Empty,
            customer?.DisplayName ?? string.Empty,
            service.Id,
            service.Title,
            appointment.StartUtc,
            appointment.EndUtc,
            timeZone.Id
        );
}

using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Queries;

/// <summary>
/// A customer asks for their own appointments.
///
/// It takes no user parameter, and that is the security design: the caller is
/// read from the token inside the handler, so there is no identifier for
/// anyone to change into somebody else's.
/// </summary>
/// <param name="IncludePast">
/// Whether to include appointments that have already happened. Off by
/// default: the answer to "what have I got booked" is about the future.
/// </param>
public sealed record ListMyAppointmentsQuery(bool IncludePast = false)
    : IRequest<IReadOnlyList<MyAppointmentDto>>;

/// <summary>
/// Handles <see cref="ListMyAppointmentsQuery"/>.
/// </summary>
public sealed class ListMyAppointmentsQueryHandler
    : IRequestHandler<ListMyAppointmentsQuery, IReadOnlyList<MyAppointmentDto>>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration.</param>
    /// <param name="currentUser">The caller, whose appointments these are.</param>
    /// <param name="timeProvider">Clock, for the cancellation cutoff.</param>
    public ListMyAppointmentsQueryHandler(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options,
        ICurrentUser currentUser,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Returns the caller's appointments.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The appointments, soonest first.</returns>
    /// <exception cref="UnauthorizedException">Thrown when nobody is signed in.</exception>
    public async Task<IReadOnlyList<MyAppointmentDto>> Handle(
        ListMyAppointmentsQuery request,
        CancellationToken cancellationToken
    )
    {
        var customerId =
            _currentUser.UserId ?? throw new UnauthorizedException("Authentication is required.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var query = _dbContext
            .Appointments.AsNoTracking()
            .Where(appointment => appointment.CustomerUserId == customerId);

        if (!request.IncludePast)
        {
            query = query.Where(appointment => appointment.EndUtc >= nowUtc);
        }

        var appointments = await query
            .OrderBy(appointment => appointment.StartUtc)
            .Join(
                _dbContext.Services.AsNoTracking(),
                appointment => appointment.ServiceId,
                service => service.Id,
                (appointment, service) => new { Appointment = appointment, service.Title }
            )
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. appointments.Select(row =>
                AppointmentMapper.ToMine(row.Appointment, row.Title, _options, _timeProvider)
            ),
        ];
    }
}

/// <summary>
/// The administration asks for the appointments in a window of time.
/// </summary>
/// <param name="FromUtc">Start of the window, UTC.</param>
/// <param name="ToUtc">End of the window, UTC.</param>
public sealed record ListAppointmentsQuery(DateTime FromUtc, DateTime ToUtc)
    : IRequest<IReadOnlyList<AdminAppointmentDto>>;

/// <summary>
/// Validation rules for <see cref="ListAppointmentsQuery"/>. The window is
/// capped for the same reason every list endpoint is paged: an unbounded one
/// is a way to make the server do arbitrary work.
/// </summary>
public sealed class ListAppointmentsQueryValidator : AbstractValidator<ListAppointmentsQuery>
{
    /// <summary>Longest window a single request may ask for.</summary>
    public const int MaxWindowDays = 120;

    /// <summary>Initializes the rules.</summary>
    public ListAppointmentsQueryValidator()
    {
        RuleFor(query => query.ToUtc).GreaterThan(query => query.FromUtc);
        RuleFor(query => query)
            .Must(query => (query.ToUtc - query.FromUtc).TotalDays <= MaxWindowDays)
            .WithMessage($"Ask for at most {MaxWindowDays} days at a time.");
    }
}

/// <summary>
/// Handles <see cref="ListAppointmentsQuery"/>.
///
/// It also works out which REQUESTS are doomed: a request overlapping an
/// already-confirmed appointment can never be confirmed, because the database
/// would refuse it. Marking it in the payload lets the calendar show it as
/// such, instead of letting an administrator discover it by clicking.
/// </summary>
public sealed class ListAppointmentsQueryHandler
    : IRequestHandler<ListAppointmentsQuery, IReadOnlyList<AdminAppointmentDto>>
{
    private readonly AppointmentsDbContext _dbContext;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    public ListAppointmentsQueryHandler(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the appointments overlapping the window.
    /// </summary>
    /// <param name="request">The validated query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The appointments, soonest first.</returns>
    public async Task<IReadOnlyList<AdminAppointmentDto>> Handle(
        ListAppointmentsQuery request,
        CancellationToken cancellationToken
    )
    {
        // Normalized before it reaches the query: a DateTime bound from a
        // query string arrives with no Kind, and PostgreSQL refuses one in a
        // timestamptz comparison.
        var fromUtc = UtcInstant.From(request.FromUtc);
        var toUtc = UtcInstant.From(request.ToUtc);

        // Overlap, not containment: an appointment that starts before the
        // window and runs into it belongs on the calendar of that window.
        var appointments = await _dbContext
            .Appointments.AsNoTracking()
            .Include(appointment => appointment.History)
            .Where(appointment => appointment.EndUtc > fromUtc && appointment.StartUtc < toUtc)
            .OrderBy(appointment => appointment.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (appointments.Count == 0)
        {
            return [];
        }

        var serviceIds = appointments.Select(a => a.ServiceId).Distinct().ToList();
        var customerIds = appointments.Select(a => a.CustomerUserId).Distinct().ToList();

        var services = await _dbContext
            .Services.AsNoTracking()
            .Where(service => serviceIds.Contains(service.Id))
            .ToDictionaryAsync(service => service.Id, cancellationToken)
            .ConfigureAwait(false);

        var customers = await _dbContext
            .Customers.AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, cancellationToken)
            .ConfigureAwait(false);

        var confirmed = appointments
            .Where(appointment => appointment.Status == AppointmentStatus.Confirmed)
            .ToList();

        return
        [
            .. appointments.Select(appointment =>
                AppointmentMapper.ToAdmin(
                    appointment,
                    services.TryGetValue(appointment.ServiceId, out var service)
                        ? service.Title
                        // A service this module never heard about: the
                        // appointment is still real, and hiding it would be
                        // worse than naming it vaguely.
                        : "—",
                    customers.GetValueOrDefault(appointment.CustomerUserId),
                    hasConflict: appointment.Status == AppointmentStatus.Requested
                        && confirmed.Exists(other =>
                            other.StartUtc < appointment.EndUtc
                            && appointment.StartUtc < other.EndUtc
                        )
                )
            ),
        ];
    }
}

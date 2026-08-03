using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Features.Booking;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Administration;

/// <summary>
/// An administrator moves an appointment to another time.
/// </summary>
/// <param name="AppointmentId">Appointment to move.</param>
/// <param name="StartUtc">New start instant, UTC.</param>
public sealed record RescheduleAppointmentCommand(Guid AppointmentId, DateTime StartUtc)
    : IRequest<AdminAppointmentDto>;

/// <summary>Validation rules for <see cref="RescheduleAppointmentCommand"/>.</summary>
public sealed class RescheduleAppointmentCommandValidator
    : AbstractValidator<RescheduleAppointmentCommand>
{
    /// <summary>Initializes the rules.</summary>
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(command => command.AppointmentId).NotEmpty();
        RuleFor(command => command.StartUtc).NotEmpty();
    }
}

/// <summary>
/// Handles <see cref="RescheduleAppointmentCommand"/>.
///
/// The move is **always revalidated here**, whatever the calendar showed: a
/// drag in the browser is an intention, not an authorization, and the state
/// it was dragged against is minutes old. The database's exclusion constraint
/// is what actually decides, so the client can be told plainly that the slot
/// is taken and put the appointment back where it was.
///
/// The appointment keeps its duration: an administrator moving a booking is
/// choosing WHEN, not redefining what was sold.
///
/// Deliberately NOT revalidated against the opening hours. Availability
/// describes what the public is offered; an administrator squeezing someone
/// in at 19:30 is making a decision the rules exist to shape, not to forbid.
/// </summary>
public sealed class RescheduleAppointmentCommandHandler
    : IRequestHandler<RescheduleAppointmentCommand, AdminAppointmentDto>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration.</param>
    /// <param name="currentUser">Administrator performing the action.</param>
    /// <param name="eventBus">Bus the public events are published on.</param>
    public RescheduleAppointmentCommandHandler(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options,
        ICurrentUser currentUser,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _currentUser = currentUser;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Moves the appointment.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The moved appointment.</returns>
    /// <exception cref="NotFoundException">Thrown when the appointment does not exist.</exception>
    /// <exception cref="BusinessException">
    /// Thrown when the appointment is closed, or when the target time is
    /// already occupied.
    /// </exception>
    public async Task<AdminAppointmentDto> Handle(
        RescheduleAppointmentCommand request,
        CancellationToken cancellationToken
    )
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var appointment =
            await _dbContext
                .Appointments.Include(entity => entity.History)
                .SingleOrDefaultAsync(
                    entity => entity.Id == request.AppointmentId,
                    cancellationToken
                )
                .ConfigureAwait(false)
            ?? throw new NotFoundException("Appointment", request.AppointmentId);

        var previousStartUtc = appointment.StartUtc;
        var duration = appointment.EndUtc - appointment.StartUtc;
        var startUtc = UtcInstant.From(request.StartUtc);

        if (!appointment.Reschedule(startUtc, startUtc + duration, actorId))
        {
            throw new BusinessException(
                $"An appointment that is {appointment.Status} cannot be moved."
            );
        }

        await SlotConflict
            .SaveOrConflictAsync(
                () => _dbContext.SaveChangesAsync(cancellationToken),
                "Another appointment already occupies that time."
            )
            .ConfigureAwait(false);

        var service = await _dbContext
            .Services.AsNoTracking()
            .SingleAsync(entity => entity.Id == appointment.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        var customer = await _dbContext
            .Customers.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == appointment.CustomerUserId, cancellationToken)
            .ConfigureAwait(false);

        await _eventBus
            .PublishAsync(
                new AppointmentRescheduled(
                    RequestAppointmentCommandHandler.Summarise(
                        appointment,
                        service,
                        customer,
                        _options.ResolveTimeZone()
                    ),
                    previousStartUtc,
                    MovedByCustomer: false
                ),
                cancellationToken
            )
            .ConfigureAwait(false);

        return AppointmentMapper.ToAdmin(appointment, service.Title, customer, hasConflict: false);
    }
}

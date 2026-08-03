using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Features.Booking;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Administration;

/// <summary>
/// Calls an appointment off.
///
/// One command for both sides, with <paramref name="ByCustomer"/> deciding
/// who is allowed and who gets told. Two commands would be two copies of the
/// same state machine, and the day they disagree is the day a customer
/// cancels someone else's booking.
/// </summary>
/// <param name="AppointmentId">Appointment to call off.</param>
/// <param name="Reason">Client-safe explanation, shown to the customer.</param>
/// <param name="ByCustomer">
/// Whether the caller is the customer. When set, the handler checks ownership
/// and the cancellation cutoff; when clear, it requires nothing more than the
/// permission the endpoint already demanded.
/// </param>
public sealed record CancelAppointmentCommand(Guid AppointmentId, string Reason, bool ByCustomer)
    : IRequest;

/// <summary>Validation rules for <see cref="CancelAppointmentCommand"/>.</summary>
public sealed class CancelAppointmentCommandValidator
    : AbstractValidator<CancelAppointmentCommand>
{
    /// <summary>Initializes the rules.</summary>
    public CancelAppointmentCommandValidator()
    {
        RuleFor(command => command.AppointmentId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(300);
    }
}

/// <summary>
/// Handles <see cref="CancelAppointmentCommand"/>.
/// </summary>
public sealed class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand>
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
    /// <param name="currentUser">Caller.</param>
    /// <param name="eventBus">Bus the public events are published on.</param>
    /// <param name="timeProvider">Clock, for the cancellation cutoff.</param>
    public CancelAppointmentCommandHandler(
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
    /// Cancels the appointment.
    /// </summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="NotFoundException">
    /// Thrown when the appointment does not exist — or, for a customer, when
    /// it is not theirs. The same answer for both, on purpose: a different
    /// one would let anyone probe which identifiers are in use.
    /// </exception>
    /// <exception cref="BusinessException">
    /// Thrown when the status forbids it, or when a customer is past the
    /// self-service cutoff.
    /// </exception>
    public async Task Handle(
        CancelAppointmentCommand request,
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

        if (request.ByCustomer)
        {
            if (appointment.CustomerUserId != actorId)
            {
                throw new NotFoundException("Appointment", request.AppointmentId);
            }

            // Re-checked here even though the DTO already told the client:
            // that value decides what is rendered, this one decides what is
            // allowed, and only one of the two is a boundary.
            if (!AppointmentMapper.CanCustomerCancel(appointment, _options, _timeProvider))
            {
                throw new BusinessException(
                    $"This appointment can no longer be cancelled online — "
                        + $"there are less than {_options.CustomerCancellationCutoffHours} hours to go. "
                        + "Please call us."
                );
            }
        }

        if (!appointment.Cancel(request.Reason, actorId))
        {
            throw new BusinessException(
                $"An appointment that is {appointment.Status} cannot be cancelled."
            );
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var service = await _dbContext
            .Services.AsNoTracking()
            .SingleAsync(entity => entity.Id == appointment.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        var customer = await _dbContext
            .Customers.AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == appointment.CustomerUserId,
                cancellationToken
            )
            .ConfigureAwait(false);

        await _eventBus
            .PublishAsync(
                new AppointmentCancelled(
                    RequestAppointmentCommandHandler.Summarise(
                        appointment,
                        service,
                        customer,
                        _options.ResolveTimeZone()
                    ),
                    request.Reason,
                    request.ByCustomer
                ),
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}

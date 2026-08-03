using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Features.Booking;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Administration;

/// <summary>
/// An administrator accepts a request.
/// </summary>
/// <param name="AppointmentId">Request to accept.</param>
public sealed record ConfirmAppointmentCommand(Guid AppointmentId) : IRequest<AdminAppointmentDto>;

/// <summary>
/// Handles <see cref="ConfirmAppointmentCommand"/>.
///
/// This is where the module's concurrency design comes together, and it does
/// three things in one transaction:
///
/// 1. **Confirms** the request. Only now does the appointment occupy its
///    slot — and only now can the exclusion constraint refuse it.
/// 2. **Cancels the requests it makes impossible.** Two people may ask for
///    the same hour; accepting one means the others can never happen, and
///    leaving them pending is people waiting for an answer that will not
///    come.
/// 3. **Announces both outcomes**, so everyone involved hears about it.
///
/// If the constraint refuses, the caller gets a sentence about the slot being
/// taken rather than a five-hundred: two administrators confirming
/// overlapping requests at the same instant is a legitimate thing to do, and
/// exactly one of them has to lose.
///
/// The whole thing runs inside one transaction, serialized by an advisory
/// lock (see <see cref="ConfirmationLockKey"/>): the loser then finds its own
/// request already cancelled by the winner and is told so, instead of both
/// transactions deadlocking on each other's rows.
/// </summary>
public sealed class ConfirmAppointmentCommandHandler
    : IRequestHandler<ConfirmAppointmentCommand, AdminAppointmentDto>
{
    /// <summary>Reason recorded on the requests that lose the slot.</summary>
    public const string SlotTakenReason = "The slot was given to another booking.";

    /// <summary>
    /// Key of the advisory lock that serializes confirmations.
    ///
    /// Confirming does two writes that another confirmation may want in the
    /// opposite order — its own appointment, and the requests it displaces —
    /// which is the textbook recipe for a deadlock: A locks X then wants Y, B
    /// locks Y then wants X, and PostgreSQL kills one with an error nobody can
    /// act on.
    ///
    /// Serializing them removes the cycle instead of racing it. It is
    /// affordable precisely here: confirming is a person clicking a button a
    /// few times an hour, not a hot path, and correctness is worth far more
    /// than the concurrency being given up. Everything else in this module —
    /// availability, booking, the calendar — stays fully concurrent.
    /// </summary>
    private const long ConfirmationLockKey = 0x4150_5054; // "APPT"

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
    public ConfirmAppointmentCommandHandler(
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
    /// Confirms the request and clears the ones it displaces.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The confirmed appointment.</returns>
    /// <exception cref="NotFoundException">Thrown when the appointment does not exist.</exception>
    /// <exception cref="BusinessException">
    /// Thrown when its status forbids confirmation, or when the slot was taken
    /// by a competing confirmation in the meantime.
    /// </exception>
    public async Task<AdminAppointmentDto> Handle(
        ConfirmAppointmentCommand request,
        CancellationToken cancellationToken
    )
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        await using var transaction = await _dbContext
            .Database.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Taken BEFORE anything is read: a lock acquired after the load would
        // leave the read itself racing, and the whole point is that the second
        // confirmation sees the first one's outcome rather than the state that
        // preceded it. Released when the transaction ends, whichever way it
        // ends.
        await _dbContext
            .Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({ConfirmationLockKey})",
                cancellationToken
            )
            .ConfigureAwait(false);

        var appointment =
            await _dbContext
                .Appointments.Include(entity => entity.History)
                .SingleOrDefaultAsync(
                    entity => entity.Id == request.AppointmentId,
                    cancellationToken
                )
                .ConfigureAwait(false)
            ?? throw new NotFoundException("Appointment", request.AppointmentId);

        if (!appointment.Confirm(actorId))
        {
            throw new BusinessException(
                $"An appointment that is {appointment.Status} cannot be confirmed."
            );
        }

        // Exactly the requests the database would now refuse: a raw overlap,
        // without the buffer. The buffer is a preference about how the
        // calendar is OFFERED; cancelling someone's request over a preference
        // would be taking a decision the administrator did not make.
        var displaced = await _dbContext
            .Appointments.Include(entity => entity.History)
            .Where(entity =>
                entity.Id != appointment.Id
                && entity.Status == AppointmentStatus.Requested
                && entity.StartUtc < appointment.EndUtc
                && appointment.StartUtc < entity.EndUtc
            )
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var loser in displaced)
        {
            loser.Cancel(SlotTakenReason, actorId);
        }

        await SlotConflict
            .SaveOrConflictAsync(
                () => _dbContext.SaveChangesAsync(cancellationToken),
                "That slot has just been taken by another confirmation. Reload the calendar."
            )
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var timeZone = _options.ResolveTimeZone();
        var service = await _dbContext
            .Services.AsNoTracking()
            .SingleAsync(entity => entity.Id == appointment.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        // One query for every customer involved, rather than one per
        // appointment: a popular hour can easily have a handful of losers.
        var customerIds = displaced
            .Select(entity => entity.CustomerUserId)
            .Append(appointment.CustomerUserId)
            .Distinct()
            .ToList();

        var customers = await _dbContext
            .Customers.AsNoTracking()
            .Where(entity => customerIds.Contains(entity.Id))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken)
            .ConfigureAwait(false);

        await _eventBus
            .PublishAsync(
                new AppointmentConfirmed(
                    RequestAppointmentCommandHandler.Summarise(
                        appointment,
                        service,
                        customers.GetValueOrDefault(appointment.CustomerUserId),
                        timeZone
                    )
                ),
                cancellationToken
            )
            .ConfigureAwait(false);

        foreach (var loser in displaced)
        {
            var loserService = await _dbContext
                .Services.AsNoTracking()
                .SingleAsync(entity => entity.Id == loser.ServiceId, cancellationToken)
                .ConfigureAwait(false);

            await _eventBus
                .PublishAsync(
                    new AppointmentCancelled(
                        RequestAppointmentCommandHandler.Summarise(
                            loser,
                            loserService,
                            customers.GetValueOrDefault(loser.CustomerUserId),
                            timeZone
                        ),
                        SlotTakenReason,
                        CancelledByCustomer: false
                    ),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        return AppointmentMapper.ToAdmin(
            appointment,
            service.Title,
            customers.GetValueOrDefault(appointment.CustomerUserId),
            hasConflict: false
        );
    }
}

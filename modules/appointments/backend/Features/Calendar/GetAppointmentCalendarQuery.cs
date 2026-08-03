using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Application.Exceptions;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Contracts.Ics;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Options;
using EnterpriseFramework.Modules.Appointments.Persistence;
using EnterpriseFramework.Modules.Authorization.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Appointments.Features.Calendar;

/// <summary>
/// Asks for one appointment as an iCalendar document.
/// </summary>
/// <param name="AppointmentId">Appointment to export.</param>
public sealed record GetAppointmentCalendarQuery(Guid AppointmentId) : IRequest<string>;

/// <summary>
/// Handles <see cref="GetAppointmentCalendarQuery"/>.
///
/// The `.ics` is built by the SAME writer the confirmation email uses, on the
/// server. Rebuilding it in the browser would mean a second implementation of
/// the format — and the two would drift on exactly the detail that matters,
/// the `UID` and `SEQUENCE` that let a calendar UPDATE an entry instead of
/// duplicating it.
///
/// Access is the customer's own appointment, or anyone who may read the
/// calendar. Note what is NOT in the document: the internal note, the phone
/// number, the other party's details. An `.ics` gets forwarded, saved to
/// disk and synced to phones, so it carries the appointment and nothing more.
/// </summary>
public sealed class GetAppointmentCalendarQueryHandler
    : IRequestHandler<GetAppointmentCalendarQuery, string>
{
    private readonly AppointmentsDbContext _dbContext;
    private readonly AppointmentsOptions _options;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    /// <param name="dbContext">Appointments persistence.</param>
    /// <param name="options">Module configuration, for the business time zone.</param>
    /// <param name="currentUser">Caller, for the ownership check.</param>
    public GetAppointmentCalendarQueryHandler(
        AppointmentsDbContext dbContext,
        IOptions<AppointmentsOptions> options,
        ICurrentUser currentUser
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Builds the calendar document.
    /// </summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The .ics content.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the appointment does not exist, or belongs to somebody else
    /// and the caller cannot read the calendar. The same answer for both: a
    /// different one would let anyone probe which identifiers exist.
    /// </exception>
    public async Task<string> Handle(
        GetAppointmentCalendarQuery request,
        CancellationToken cancellationToken
    )
    {
        var appointment =
            await _dbContext
                .Appointments.AsNoTracking()
                .SingleOrDefaultAsync(
                    entity => entity.Id == request.AppointmentId,
                    cancellationToken
                )
                .ConfigureAwait(false)
            ?? throw new NotFoundException("Appointment", request.AppointmentId);

        var isOwner = appointment.CustomerUserId == _currentUser.UserId;
        var mayReadCalendar = _currentUser.Permissions.Contains(
            Permissions.Appointments.Read,
            StringComparer.Ordinal
        );

        if (!isOwner && !mayReadCalendar)
        {
            throw new NotFoundException("Appointment", request.AppointmentId);
        }

        var service = await _dbContext
            .Services.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == appointment.ServiceId, cancellationToken)
            .ConfigureAwait(false);

        var customer = await _dbContext
            .Customers.AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == appointment.CustomerUserId,
                cancellationToken
            )
            .ConfigureAwait(false);

        var summary = new AppointmentSummary(
            appointment.Id,
            appointment.CustomerUserId,
            customer?.Email ?? string.Empty,
            customer?.DisplayName ?? string.Empty,
            appointment.ServiceId,
            service?.Title ?? "Appuntamento",
            appointment.StartUtc,
            appointment.EndUtc,
            _options.ResolveTimeZone().Id
        );

        // The sequence grows with every change, so a file downloaded after a
        // reschedule supersedes the one attached to the original
        // confirmation. The history length is a monotonic counter that costs
        // nothing to keep — it only ever grows, which is exactly the contract
        // SEQUENCE asks for.
        var sequence = await _dbContext
            .History.AsNoTracking()
            .CountAsync(entry => entry.AppointmentId == appointment.Id, cancellationToken)
            .ConfigureAwait(false);

        return IcsCalendar.Build(
            summary,
            _options.ResolveTimeZone().StandardName,
            sequence,
            cancelled: appointment.Status == AppointmentStatus.Cancelled
        );
    }
}

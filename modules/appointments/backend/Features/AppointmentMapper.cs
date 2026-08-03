using EnterpriseFramework.Modules.Appointments.Contracts;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Options;

namespace EnterpriseFramework.Modules.Appointments.Features;

/// <summary>
/// Turns appointments into the two shapes clients see.
///
/// The two shapes are separate types rather than one with optional fields,
/// and this is the only place that builds either: an administrative field
/// added later cannot reach a customer by someone forgetting to strip it,
/// because the customer's shape has nowhere to put it.
/// </summary>
public static class AppointmentMapper
{
    /// <summary>
    /// Maps an appointment for the person who booked it.
    /// </summary>
    /// <param name="appointment">The appointment.</param>
    /// <param name="serviceTitle">Title from this module's projection.</param>
    /// <param name="options">Module configuration, for the cancellation cutoff.</param>
    /// <param name="timeProvider">Clock, to evaluate that cutoff.</param>
    /// <returns>The customer-facing DTO.</returns>
    public static MyAppointmentDto ToMine(
        Appointment appointment,
        string serviceTitle,
        AppointmentsOptions options,
        TimeProvider timeProvider
    ) =>
        new(
            appointment.Id,
            appointment.ServiceId,
            serviceTitle,
            appointment.StartUtc,
            appointment.EndUtc,
            appointment.Status,
            appointment.ContactPhone,
            appointment.CustomerNote,
            appointment.CancellationReason,
            CanCustomerCancel(appointment, options, timeProvider),
            appointment.CreatedAtUtc
        );

    /// <summary>
    /// Tells whether the customer may still call the appointment off
    /// themselves.
    ///
    /// Computed server-side and sent to the client so the button matches what
    /// the API will accept — but the API checks it again on the way in. This
    /// value decides what is RENDERED; it never decides what is allowed.
    /// </summary>
    /// <param name="appointment">The appointment.</param>
    /// <param name="options">Module configuration, for the cutoff.</param>
    /// <param name="timeProvider">Clock.</param>
    /// <returns><c>true</c> when self-service cancellation is still open.</returns>
    public static bool CanCustomerCancel(
        Appointment appointment,
        AppointmentsOptions options,
        TimeProvider timeProvider
    )
    {
        if (!AppointmentTransitions.IsAllowed(appointment.Status, AppointmentStatus.Cancelled))
        {
            return false;
        }

        var cutoff = appointment.StartUtc.AddHours(-options.CustomerCancellationCutoffHours);
        return timeProvider.GetUtcNow().UtcDateTime < cutoff;
    }

    /// <summary>
    /// Maps an appointment for the administration.
    /// </summary>
    /// <param name="appointment">The appointment, with its history loaded.</param>
    /// <param name="serviceTitle">Title from this module's projection.</param>
    /// <param name="customer">Customer projection, if known.</param>
    /// <param name="hasConflict">
    /// Whether this request overlaps an already-confirmed appointment.
    /// </param>
    /// <returns>The administrative DTO.</returns>
    public static AdminAppointmentDto ToAdmin(
        Appointment appointment,
        string serviceTitle,
        CustomerProjection? customer,
        bool hasConflict
    ) =>
        new(
            appointment.Id,
            appointment.ServiceId,
            serviceTitle,
            appointment.CustomerUserId,
            customer?.DisplayName ?? "—",
            customer?.Email ?? string.Empty,
            appointment.StartUtc,
            appointment.EndUtc,
            appointment.Status,
            appointment.ContactPhone,
            appointment.CustomerNote,
            appointment.AdminNote,
            appointment.CancellationReason,
            hasConflict,
            appointment.CreatedAtUtc,
            [
                .. appointment
                    .History.OrderBy(entry => entry.AtUtc)
                    .Select(AppointmentHistoryDto.FromEntry),
            ]
        );
}

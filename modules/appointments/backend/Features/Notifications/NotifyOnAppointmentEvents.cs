using System.Globalization;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Notifications.Contracts.Events;
using MediatR;

namespace EnterpriseFramework.Modules.Appointments.Features.Notifications;

/// <summary>
/// Turns this module's own public events into in-app notifications.
///
/// **No transport is written here.** The Notifications module already
/// publishes <c>NotificationRequested</c> as a public contract, and the
/// notification it stores is itself a realtime event — so publishing one
/// object produces the bell in the UI and the live push, with this module
/// knowing about neither SignalR nor a notifications table.
///
/// It subscribes to its OWN events rather than notifying inline from the
/// handlers, and that is deliberate: the handlers stay about appointments,
/// and "who gets told what" lives in one readable place. Removing the
/// Notifications module removes this behaviour and nothing else.
///
/// The customer is notified here; the administrator's side is
/// <see cref="AppointmentLiveEvent"/>, which reaches everyone watching the
/// calendar without naming anybody.
/// </summary>
public sealed class NotifyOnAppointmentEvents
    : INotificationHandler<DomainEventNotification<AppointmentRequested>>,
        INotificationHandler<DomainEventNotification<AppointmentConfirmed>>,
        INotificationHandler<DomainEventNotification<AppointmentRescheduled>>,
        INotificationHandler<DomainEventNotification<AppointmentCancelled>>,
        INotificationHandler<DomainEventNotification<AppointmentReminderDue>>
{
    /// <summary>Where a notification about an appointment takes the customer.</summary>
    private const string CustomerLink = "/my-appointments";

    private readonly IEventBus _eventBus;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="eventBus">Bus the notification requests are published on.</param>
    public NotifyOnAppointmentEvents(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public Task Handle(
        DomainEventNotification<AppointmentRequested> notification,
        CancellationToken cancellationToken
    ) =>
        PublishAsync(
            notification.DomainEvent.Appointment,
            "Request sent",
            // The wording carries the whole design decision: a request is not
            // a booking, and the interface must never suggest it is.
            $"We have your request for {notification.DomainEvent.Appointment.ServiceTitle}. "
                + "You will hear from us once it is confirmed.",
            NotificationLevel.Info,
            live: true,
            cancellationToken
        );

    /// <inheritdoc />
    public Task Handle(
        DomainEventNotification<AppointmentConfirmed> notification,
        CancellationToken cancellationToken
    ) =>
        PublishAsync(
            notification.DomainEvent.Appointment,
            "Appointment confirmed",
            $"{notification.DomainEvent.Appointment.ServiceTitle} on "
                + $"{Local(notification.DomainEvent.Appointment)}.",
            NotificationLevel.Success,
            live: true,
            cancellationToken
        );

    /// <inheritdoc />
    public Task Handle(
        DomainEventNotification<AppointmentRescheduled> notification,
        CancellationToken cancellationToken
    ) =>
        PublishAsync(
            notification.DomainEvent.Appointment,
            "Appointment moved",
            $"{notification.DomainEvent.Appointment.ServiceTitle} is now on "
                + $"{Local(notification.DomainEvent.Appointment)}.",
            NotificationLevel.Warning,
            live: true,
            cancellationToken
        );

    /// <inheritdoc />
    public Task Handle(
        DomainEventNotification<AppointmentCancelled> notification,
        CancellationToken cancellationToken
    ) =>
        PublishAsync(
            notification.DomainEvent.Appointment,
            "Appointment cancelled",
            notification.DomainEvent.Reason,
            NotificationLevel.Warning,
            live: true,
            cancellationToken
        );

    /// <inheritdoc />
    public Task Handle(
        DomainEventNotification<AppointmentReminderDue> notification,
        CancellationToken cancellationToken
    ) =>
        PublishAsync(
            notification.DomainEvent.Appointment,
            notification.DomainEvent.Kind == ReminderKind.DayBefore
                ? "Tomorrow"
                : "Later today",
            $"{notification.DomainEvent.Appointment.ServiceTitle} on "
                + $"{Local(notification.DomainEvent.Appointment)}.",
            NotificationLevel.Info,
            // A reminder does not need to interrupt: it lands in the bell for
            // when the person next looks, which is the point of a reminder.
            live: false,
            cancellationToken
        );

    /// <summary>
    /// Notifies the customer, and optionally tells the calendar it changed.
    /// </summary>
    private async Task PublishAsync(
        AppointmentSummary appointment,
        string title,
        string body,
        NotificationLevel level,
        bool live,
        CancellationToken cancellationToken
    )
    {
        await _eventBus
            .PublishAsync(
                new NotificationRequested(
                    appointment.CustomerUserId,
                    title,
                    body,
                    level,
                    CustomerLink
                ),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (live)
        {
            await _eventBus
                .PublishAsync(
                    new AppointmentLiveEvent(appointment.AppointmentId, appointment.StartUtc),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }

    private static string Local(AppointmentSummary appointment)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(appointment.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(appointment.StartUtc, zone);

        return local.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Tells everyone watching the calendar that something moved.
///
/// It carries an identifier and an instant and nothing else, on purpose: the
/// audience is a GROUP, so whatever this object holds is delivered to every
/// administrator connected — and in a multi-tenant deployment that group
/// spans tenants, a caveat the Realtime module documents. A payload with a
/// customer's name in it would be a leak; a payload that says "reload" is not.
///
/// The client reacts by invalidating its calendar query, which is both the
/// simplest and the most correct behaviour: it re-reads through the endpoint
/// that already enforces the permissions.
/// </summary>
/// <param name="AppointmentId">What changed.</param>
/// <param name="StartUtc">When it sits, so a client can ignore other periods.</param>
public sealed record AppointmentLiveEvent(Guid AppointmentId, DateTime StartUtc) : IRealtimeEvent
{
    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    /// <summary>
    /// Everyone in the administrators' group. Assigned server-side at
    /// connection time — a client cannot ask to join it.
    /// </summary>
    public RealtimeAudience Audience => RealtimeAudience.ForGroup("role:Admin");

    /// <inheritdoc />
    public string Channel => "appointment.changed";
}

using System.Globalization;
using System.Net;
using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;
using EnterpriseFramework.Modules.Appointments.Contracts.Ics;
using EnterpriseFramework.Modules.Email.Abstractions;
using EnterpriseFramework.Modules.Email.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace EnterpriseFramework.Modules.Email.Features.Appointments;

/// <summary>
/// Turns the Appointments module's public events into messages for the
/// customer.
///
/// Appointments has no idea email exists: it announces what happened, and
/// this module decides that "what happened" is worth writing about. Disable
/// this module and bookings still work — they just stop being confirmed in
/// writing, which is exactly the trade a project makes when it removes it.
///
/// Everything is composed from the EVENT alone. No lookups, no queries into
/// another module's tables: the summary carries the customer's address, the
/// service title and the times precisely so that stays true.
///
/// Every message goes to the OUTBOX, never sent inline, so a slow mail server
/// cannot make an administrator's click hang.
/// </summary>
public sealed class SendAppointmentEmails
    : INotificationHandler<DomainEventNotification<AppointmentRequested>>,
        INotificationHandler<DomainEventNotification<AppointmentConfirmed>>,
        INotificationHandler<DomainEventNotification<AppointmentRescheduled>>,
        INotificationHandler<DomainEventNotification<AppointmentCancelled>>,
        INotificationHandler<DomainEventNotification<AppointmentReminderDue>>
{
    private readonly IEmailOutbox _outbox;
    private readonly EmailOptions _options;

    /// <summary>
    /// Initializes the subscriber.
    /// </summary>
    /// <param name="outbox">Outbox that delivers in the background.</param>
    /// <param name="options">Email configuration, for the sender's name.</param>
    public SendAppointmentEmails(IEmailOutbox outbox, IOptions<EmailOptions> options)
    {
        _outbox = outbox;
        _options = options.Value;
    }

    /// <summary>
    /// Acknowledges a request — and is careful to call it one.
    /// </summary>
    /// <param name="notification">The wrapped event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<AppointmentRequested> notification,
        CancellationToken cancellationToken
    )
    {
        var appointment = notification.DomainEvent.Appointment;

        // "Request received", never "booking confirmed". Until someone
        // accepts it that hour belongs to nobody, and a message that says
        // otherwise is a promise the business has not made.
        Send(
            appointment,
            subject: $"Request received — {appointment.ServiceTitle}",
            heading: "We have your request",
            body: $"We received your request for <strong>{Encoded(appointment.ServiceTitle)}</strong> "
                + $"on {Encoded(LocalWhen(appointment))}. "
                + "We will confirm it shortly — you will get another email as soon as we do.",
            attachment: null
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Confirms the appointment, with the calendar entry attached.
    /// </summary>
    /// <param name="notification">The wrapped event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<AppointmentConfirmed> notification,
        CancellationToken cancellationToken
    )
    {
        var appointment = notification.DomainEvent.Appointment;

        Send(
            appointment,
            subject: $"Confirmed — {appointment.ServiceTitle}",
            heading: "Your appointment is confirmed",
            body: $"<strong>{Encoded(appointment.ServiceTitle)}</strong> on "
                + $"{Encoded(LocalWhen(appointment))}.<br><br>"
                + $"<a href=\"{IcsCalendar.GoogleCalendarUrl(appointment, appointment.ServiceTitle)}\">"
                + "Add it to Google Calendar</a>, or open the attached file for any other calendar.",
            // Sequence 0: the first revision. A later move sends the same UID
            // with a higher one, and the entry MOVES instead of duplicating.
            attachment: Calendar(appointment, sequence: 0)
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tells the customer the appointment moved, and where from.
    /// </summary>
    /// <param name="notification">The wrapped event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<AppointmentRescheduled> notification,
        CancellationToken cancellationToken
    )
    {
        var @event = notification.DomainEvent;
        var appointment = @event.Appointment;

        Send(
            appointment,
            subject: $"Moved — {appointment.ServiceTitle}",
            heading: "Your appointment has moved",
            // Saying where it WAS is not decoration: without it the reader has
            // to open their own calendar to work out what changed.
            body: $"<strong>{Encoded(appointment.ServiceTitle)}</strong> has moved from "
                + $"{Encoded(Local(@event.PreviousStartUtc, appointment.TimeZoneId))} to "
                + $"<strong>{Encoded(LocalWhen(appointment))}</strong>.<br><br>"
                + "The attached file updates the entry already in your calendar.",
            // Sequence 1: higher than the confirmation, which is what makes a
            // calendar replace the entry rather than add a second one.
            attachment: Calendar(appointment, sequence: 1)
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tells the customer the appointment is off, and why.
    /// </summary>
    /// <param name="notification">The wrapped event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<AppointmentCancelled> notification,
        CancellationToken cancellationToken
    )
    {
        var @event = notification.DomainEvent;
        var appointment = @event.Appointment;

        // The customer already knows if they did it themselves; mailing them
        // about their own click is noise. This message is for the case where
        // the news comes from us.
        if (@event.CancelledByCustomer)
        {
            return Task.CompletedTask;
        }

        Send(
            appointment,
            subject: $"Cancelled — {appointment.ServiceTitle}",
            heading: "Your appointment has been cancelled",
            body: $"<strong>{Encoded(appointment.ServiceTitle)}</strong> on "
                + $"{Encoded(LocalWhen(appointment))} will not go ahead.<br><br>"
                + $"Reason: {Encoded(@event.Reason)}",
            attachment: Calendar(appointment, sequence: 2, cancelled: true)
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends one of the two reminders.
    /// </summary>
    /// <param name="notification">The wrapped event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task Handle(
        DomainEventNotification<AppointmentReminderDue> notification,
        CancellationToken cancellationToken
    )
    {
        var @event = notification.DomainEvent;
        var appointment = @event.Appointment;

        var heading =
            @event.Kind == ReminderKind.DayBefore ? "See you tomorrow" : "See you later today";

        Send(
            appointment,
            subject: $"Reminder — {appointment.ServiceTitle}",
            heading: heading,
            body: $"<strong>{Encoded(appointment.ServiceTitle)}</strong> on "
                + $"{Encoded(LocalWhen(appointment))}.",
            attachment: null
        );

        return Task.CompletedTask;
    }

    /// <summary>Builds the message and hands it to the outbox.</summary>
    private void Send(
        AppointmentSummary appointment,
        string subject,
        string heading,
        string body,
        EmailAttachment? attachment
    )
    {
        if (string.IsNullOrWhiteSpace(appointment.CustomerEmail))
        {
            // No address to write to. It happens when the customer projection
            // has not caught up; dropping the message beats throwing inside an
            // event handler and failing the administrator's request.
            return;
        }

        var html = $"<h2>{Encoded(heading)}</h2><p>{body}</p>";
        var text = $"{heading}\n\n{WebUtility.HtmlDecode(StripTags(body))}";

        _outbox.Add(
            new EmailMessage(
                appointment.CustomerEmail,
                subject,
                html,
                text,
                attachment is null ? null : [attachment]
            )
        );
    }

    private EmailAttachment Calendar(
        AppointmentSummary appointment,
        int sequence,
        bool cancelled = false
    ) =>
        new(
            "appointment.ics",
            IcsCalendar.ContentType,
            IcsCalendar.Build(appointment, _options.FromName, sequence, cancelled)
        );

    /// <summary>The appointment's start, rendered in the BUSINESS time zone.</summary>
    private static string LocalWhen(AppointmentSummary appointment) =>
        Local(appointment.StartUtc, appointment.TimeZoneId);

    /// <summary>
    /// Renders an instant in the business time zone.
    ///
    /// Not the customer's zone, and not the server's: the appointment happens
    /// where the business is, and "09:00" has to mean the time they will read
    /// on the door.
    /// </summary>
    private static string Local(DateTime utc, string timeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);

        return local.ToString("dddd d MMMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Everything the visitor typed is encoded before it reaches HTML.</summary>
    private static string Encoded(string value) => WebUtility.HtmlEncode(value);

    /// <summary>Crude tag strip for the plain-text alternative.</summary>
    private static string StripTags(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}

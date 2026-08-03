using System.Globalization;
using System.Text;
using EnterpriseFramework.Modules.Appointments.Contracts.Events;

namespace EnterpriseFramework.Modules.Appointments.Contracts.Ics;

/// <summary>
/// Writes an appointment as an iCalendar (<c>.ics</c>) document, and builds
/// the "add to Google Calendar" link.
///
/// It lives in the CONTRACTS assembly, next to the events, because whoever
/// wants to attach a calendar file to a message — the Email module today —
/// must be able to build one from the event alone, without referencing the
/// Appointments implementation.
///
/// Why iCalendar at all: it is the one format every calendar reads, it needs
/// no OAuth, no tokens to refresh and no consent a customer would rather not
/// give. Two-way synchronisation is a different product; this covers the case
/// people actually have — "put it in my calendar".
///
/// The part that earns its keep is <c>SEQUENCE</c>: sending an updated file
/// with the SAME <c>UID</c> and a higher sequence makes the customer's
/// calendar MOVE the existing entry instead of adding a second one. Without
/// it, a rescheduled appointment quietly becomes two appointments.
/// </summary>
public static class IcsCalendar
{
    /// <summary>
    /// Domain used to build the <c>UID</c>. It never resolves and never needs
    /// to: a UID only has to be globally unique and stable.
    /// </summary>
    private const string UidDomain = "appointments.enterprise-framework";

    /// <summary>MIME type an <c>.ics</c> attachment is served with.</summary>
    public const string ContentType = "text/calendar";

    /// <summary>
    /// Builds the calendar document for an appointment.
    /// </summary>
    /// <param name="appointment">Who, what and when.</param>
    /// <param name="organizerName">Name shown as the organizer.</param>
    /// <param name="sequence">
    /// Revision of this appointment: 0 on confirmation, higher every time it
    /// moves. A calendar only replaces an entry when the sequence GROWS.
    /// </param>
    /// <param name="cancelled">
    /// When set, the document CANCELS the entry instead of creating it — the
    /// customer's calendar removes it on its own.
    /// </param>
    /// <returns>The .ics content, with CRLF line endings as the format requires.</returns>
    public static string Build(
        AppointmentSummary appointment,
        string organizerName,
        int sequence = 0,
        bool cancelled = false
    )
    {
        var builder = new StringBuilder();

        builder.Append("BEGIN:VCALENDAR\r\n");
        builder.Append("VERSION:2.0\r\n");
        builder.Append("PRODID:-//Enterprise Framework//Appointments//EN\r\n");
        // REQUEST publishes or updates the entry; CANCEL withdraws it.
        builder.Append(cancelled ? "METHOD:CANCEL\r\n" : "METHOD:REQUEST\r\n");
        builder.Append("BEGIN:VEVENT\r\n");

        // Stable across every message about this appointment: it is what lets
        // a later update replace the entry rather than duplicate it.
        builder.Append(
            CultureInfo.InvariantCulture,
            $"UID:{appointment.AppointmentId}@{UidDomain}\r\n"
        );
        builder.Append(CultureInfo.InvariantCulture, $"SEQUENCE:{sequence}\r\n");
        builder.Append(cancelled ? "STATUS:CANCELLED\r\n" : "STATUS:CONFIRMED\r\n");
        builder.Append(CultureInfo.InvariantCulture, $"DTSTAMP:{Stamp(DateTime.UtcNow)}\r\n");
        builder.Append(CultureInfo.InvariantCulture, $"DTSTART:{Stamp(appointment.StartUtc)}\r\n");
        builder.Append(CultureInfo.InvariantCulture, $"DTEND:{Stamp(appointment.EndUtc)}\r\n");
        builder.Append(CultureInfo.InvariantCulture, $"SUMMARY:{Escape(appointment.ServiceTitle)}\r\n");
        builder.Append(
            CultureInfo.InvariantCulture,
            $"ORGANIZER;CN={Escape(organizerName)}:mailto:noreply@{UidDomain}\r\n"
        );
        builder.Append(
            CultureInfo.InvariantCulture,
            $"ATTENDEE;CN={Escape(appointment.CustomerName)}:mailto:{appointment.CustomerEmail}\r\n"
        );

        builder.Append("END:VEVENT\r\n");
        builder.Append("END:VCALENDAR\r\n");

        return builder.ToString();
    }

    /// <summary>
    /// Builds the URL that opens Google Calendar with the appointment
    /// pre-filled.
    ///
    /// A plain link, deliberately: adding an entry through Google's API would
    /// mean OAuth, a consent screen and refresh tokens, for something a
    /// template URL does in one click. The customer stays in control — they
    /// see what is about to be saved before saving it.
    /// </summary>
    /// <param name="appointment">Who, what and when.</param>
    /// <param name="details">Text shown in the description field.</param>
    /// <returns>An absolute URL.</returns>
    public static string GoogleCalendarUrl(AppointmentSummary appointment, string details)
    {
        var dates =
            $"{Stamp(appointment.StartUtc)}/{Stamp(appointment.EndUtc)}";

        return "https://calendar.google.com/calendar/render?action=TEMPLATE"
            + $"&text={Uri.EscapeDataString(appointment.ServiceTitle)}"
            + $"&dates={dates}"
            + $"&details={Uri.EscapeDataString(details)}";
    }

    /// <summary>Formats an instant as iCalendar UTC ("20260302T090000Z").</summary>
    private static string Stamp(DateTime utc) =>
        utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    /// <summary>
    /// Escapes the characters iCalendar treats as structure.
    ///
    /// A service called "Consulenza, prima visita" would otherwise end the
    /// property at the comma and produce a file some calendars silently
    /// refuse to import.
    /// </summary>
    private static string Escape(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}

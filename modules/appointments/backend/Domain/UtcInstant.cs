namespace EnterpriseFramework.Modules.Appointments.Domain;

/// <summary>
/// Turns whatever a caller sent into an unambiguous UTC instant.
///
/// It exists because <see cref="DateTime"/> carries a <see cref="DateTimeKind"/>
/// that almost nothing sets correctly. A value deserialized from JSON, bound
/// from a query string, or built from a <see cref="DateOnly"/> arrives as
/// <see cref="DateTimeKind.Unspecified"/> — and PostgreSQL refuses to store
/// one in a <c>timestamp with time zone</c> column, which is how this shows
/// up: not as a wrong time, but as a five-hundred on a perfectly ordinary
/// request.
///
/// The rule is the API's contract, stated once: **this module speaks UTC.** A
/// value with no zone is taken at its word rather than shifted by whatever
/// the server's own zone happens to be — silently applying the server's
/// offset is how a booking ends up an hour out on one machine and correct on
/// another.
/// </summary>
public static class UtcInstant
{
    /// <summary>
    /// Normalizes an instant to UTC.
    /// </summary>
    /// <param name="value">The value as it arrived.</param>
    /// <returns>The same instant, with <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime From(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            // Genuinely local to this process: converting is right.
            DateTimeKind.Local => value.ToUniversalTime(),
            // No zone attached: the contract says UTC, so believe it.
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    /// <summary>
    /// Builds a UTC instant from a date and a time of day.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="time">The time of day.</param>
    /// <returns>The instant, with <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime From(DateOnly date, TimeOnly time) =>
        DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Utc);
}

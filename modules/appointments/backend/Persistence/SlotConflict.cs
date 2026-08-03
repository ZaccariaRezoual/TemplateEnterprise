using EnterpriseFramework.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnterpriseFramework.Modules.Appointments.Persistence;

/// <summary>
/// Recognises the one database error this module expects to see, and turns it
/// into something a person can act on.
///
/// The error is the exclusion constraint refusing two confirmed appointments
/// on the same time. That is not a bug to be logged and hidden: it is the
/// correct outcome of two administrators confirming overlapping requests at
/// the same moment, and the loser needs to be told what happened rather than
/// shown a five-hundred.
///
/// It exists at all because **checking first does not work**. "Is anything
/// booked then?" followed by an insert has a gap between the two, and two
/// requests fit through it comfortably. The constraint is the only thing that
/// closes it, so the code's job is to expect the refusal, not to prevent it.
/// </summary>
public static class SlotConflict
{
    /// <summary>PostgreSQL SQLSTATE for a violated exclusion constraint.</summary>
    private const string ExclusionViolation = "23P01";

    /// <summary>
    /// Tells whether an exception is the slot conflict.
    /// </summary>
    /// <param name="exception">The exception raised by SaveChanges.</param>
    /// <returns><c>true</c> when the exclusion constraint refused the write.</returns>
    public static bool IsSlotConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: ExclusionViolation,
        } postgres
        && string.Equals(
            postgres.ConstraintName,
            AppointmentsDbContext.NoOverlapConstraint,
            StringComparison.Ordinal
        );

    /// <summary>
    /// Runs a save and translates the conflict.
    /// </summary>
    /// <param name="save">The persistence operation to attempt.</param>
    /// <param name="message">
    /// What to tell the caller when the slot is gone. Written for a human:
    /// they need to pick another time, not to read a constraint name.
    /// </param>
    /// <returns>A task that completes when the save succeeded.</returns>
    /// <exception cref="BusinessException">
    /// Thrown when the slot was taken between offering it and confirming it.
    /// </exception>
    public static async Task SaveOrConflictAsync(Func<Task> save, string message)
    {
        try
        {
            await save().ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsSlotConflict(exception))
        {
            throw new BusinessException(message);
        }
    }
}

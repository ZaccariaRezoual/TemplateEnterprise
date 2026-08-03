using EnterpriseFramework.Modules.Appointments.Domain;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Appointments;

/// <summary>
/// The state machine of an appointment, and the history it leaves behind.
///
/// The moves that are FORBIDDEN are the ones worth pinning down: nothing on
/// screen shows that a cancelled appointment must never come back, and the
/// day it does, a customer who was told their slot was gone finds it booked
/// again.
/// </summary>
public sealed class AppointmentTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

    private static Appointment Requested() =>
        Appointment.Request(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Start,
            Start.AddHours(1),
            "+39 333 1234567",
            "Note"
        );

    private static Appointment Confirmed()
    {
        var appointment = Requested();
        appointment.Confirm(Actor).ShouldBeTrue();
        return appointment;
    }

    [Fact]
    public void ARequestStartsUnconfirmedAndOccupiesNothing()
    {
        var appointment = Requested();

        appointment.Status.ShouldBe(AppointmentStatus.Requested);
        // The heart of the concurrency design: several people may hold a
        // request for the same hour.
        appointment.OccupiesSlot.ShouldBeFalse();
    }

    [Fact]
    public void OnlyAConfirmedAppointmentOccupiesItsSlot()
    {
        Confirmed().OccupiesSlot.ShouldBeTrue();
    }

    [Theory]
    [InlineData(AppointmentStatus.Requested, AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Requested, AppointmentStatus.Cancelled, true)]
    [InlineData(AppointmentStatus.Requested, AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Requested, AppointmentStatus.NoShow, false)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Completed, true)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.NoShow, true)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Cancelled, true)]
    [InlineData(AppointmentStatus.Cancelled, AppointmentStatus.Confirmed, false)]
    [InlineData(AppointmentStatus.Cancelled, AppointmentStatus.Cancelled, false)]
    [InlineData(AppointmentStatus.Completed, AppointmentStatus.Cancelled, false)]
    [InlineData(AppointmentStatus.NoShow, AppointmentStatus.Confirmed, false)]
    public void TheTransitionTableSaysWhatMayHappen(
        AppointmentStatus from,
        AppointmentStatus to,
        bool allowed
    )
    {
        AppointmentTransitions.IsAllowed(from, to).ShouldBe(allowed);
    }

    [Fact]
    public void ACancelledAppointmentCannotBeRevived()
    {
        var appointment = Requested();
        appointment.Cancel("Changed my mind", Actor).ShouldBeTrue();

        // Reviving it would give back a slot the customer was already told
        // they had lost — and which somebody else may now hold.
        appointment.Confirm(Actor).ShouldBeFalse();
        appointment.Status.ShouldBe(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void ARequestCannotBeMarkedAsANoShow()
    {
        // Nobody fails to turn up to an appointment nobody accepted.
        Requested().MarkNoShow(Actor).ShouldBeFalse();
    }

    [Fact]
    public void AClosedAppointmentCannotBeMoved()
    {
        var appointment = Confirmed();
        appointment.Cancel("Called off", Actor).ShouldBeTrue();

        appointment.Reschedule(Start.AddDays(1), Start.AddDays(1).AddHours(1), Actor)
            .ShouldBeFalse();
    }

    [Fact]
    public void MovingAnAppointmentKeepsItsDurationAndItsStatus()
    {
        var appointment = Confirmed();
        var newStart = Start.AddDays(1);

        appointment.Reschedule(newStart, newStart.AddHours(1), Actor).ShouldBeTrue();

        appointment.StartUtc.ShouldBe(newStart);
        appointment.EndUtc.ShouldBe(newStart.AddHours(1));
        appointment.Status.ShouldBe(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void EveryChangeLeavesALineInTheHistory()
    {
        var appointment = Requested();
        appointment.Confirm(Actor);
        appointment.Reschedule(Start.AddHours(2), Start.AddHours(3), Actor);
        appointment.Cancel("No longer needed", Actor);

        // "You moved it, not me" is a conversation only a log ends.
        appointment.History.Count.ShouldBe(4);
        appointment.History.First().FromStatus.ShouldBeNull();
        appointment.History.Last().ToStatus.ShouldBe(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void TheHistoryRecordsWhereAnAppointmentMovedFromAndTo()
    {
        var appointment = Confirmed();
        var newStart = Start.AddHours(2);

        appointment.Reschedule(newStart, newStart.AddHours(1), Actor);

        var move = appointment.History.Last();
        move.FromStartUtc.ShouldBe(Start);
        move.ToStartUtc.ShouldBe(newStart);
    }

    [Fact]
    public void ARefusedMoveLeavesNoTrace()
    {
        var appointment = Requested();
        appointment.Cancel("Called off", Actor);
        var linesBefore = appointment.History.Count;

        appointment.Confirm(Actor).ShouldBeFalse();

        // A refused transition must not pollute the log with things that did
        // not happen.
        appointment.History.Count.ShouldBe(linesBefore);
    }

    [Fact]
    public void TheCancellationReasonIsKeptForTheCustomerToRead()
    {
        var appointment = Requested();

        appointment.Cancel("  The slot was given to another booking.  ", Actor);

        appointment.CancellationReason.ShouldBe("The slot was given to another booking.");
    }

    [Fact]
    public void TheAdminNoteIsSeparateFromTheCustomerNote()
    {
        var appointment = Requested();

        appointment.SetAdminNote("Difficult parking, call on arrival");

        appointment.AdminNote.ShouldBe("Difficult parking, call on arrival");
        appointment.CustomerNote.ShouldBe("Note");
    }
}

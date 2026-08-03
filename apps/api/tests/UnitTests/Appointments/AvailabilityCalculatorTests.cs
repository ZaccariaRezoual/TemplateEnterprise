using System.Globalization;
using EnterpriseFramework.Modules.Appointments.Domain;
using EnterpriseFramework.Modules.Appointments.Domain.Availability;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Appointments;

/// <summary>
/// The availability engine.
///
/// These are the tests the module is really built around. Everything else can
/// be seen on screen; a slot offered an hour off, or a slot that should not
/// have been offered at all, looks completely normal until someone turns up
/// at the wrong time — and the day the clocks change is when it happens.
/// </summary>
public sealed class AvailabilityCalculatorTests
{
    /// <summary>
    /// Rome: it observes European summer time, so it exercises both the hour
    /// that does not exist in March and the one that happens twice in October.
    /// </summary>
    private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    /// <summary>A Monday, far enough away that the minimum notice never bites.</summary>
    private static readonly DateOnly Monday = new(2026, 3, 2);

    private static AvailabilityRequest Request(
        int durationMinutes = 60,
        int granularity = 30,
        int buffer = 0,
        int noticeHours = 0,
        int maxAdvanceDays = 365,
        DateTime? nowUtc = null,
        TimeZoneInfo? timeZone = null
    ) =>
        new(
            timeZone ?? Rome,
            durationMinutes,
            granularity,
            buffer,
            noticeHours,
            maxAdvanceDays,
            nowUtc ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        );

    private static AvailabilityRule Rule(DayOfWeek day, string from, string to) =>
        AvailabilityRule.Create(day, TimeOnly.Parse(from, CultureInfo.InvariantCulture), TimeOnly.Parse(to, CultureInfo.InvariantCulture));

    /// <summary>The local wall-clock times offered on the single returned day.</summary>
    private static IReadOnlyList<string> LocalStarts(IReadOnlyList<AvailableDay> days) =>
        [
            .. days.SelectMany(day => day.Slots)
                .Select(slot => TimeZoneInfo.ConvertTimeFromUtc(slot.StartUtc, Rome).ToString("HH:mm", CultureInfo.InvariantCulture)),
        ];

    [Fact]
    public void WalksTheWindowOnTheGrid()
    {
        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "12:00")],
            [],
            [],
            Request(durationMinutes: 60, granularity: 30)
        );

        // 11:30 is absent: an hour starting there would end at 12:30, past
        // closing.
        LocalStarts(days).ShouldBe(["09:00", "09:30", "10:00", "10:30", "11:00"]);
    }

    [Fact]
    public void ReturnsClosedDaysAsEmptyRatherThanOmittingThem()
    {
        var sunday = Monday.AddDays(-1);

        var days = AvailabilityCalculator.Calculate(
            sunday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "10:00")],
            [],
            [],
            Request()
        );

        // A calendar has to grey out Sunday. A missing day is
        // indistinguishable from a day nobody asked about.
        days.Count.ShouldBe(2);
        days[0].Date.ShouldBe(sunday);
        days[0].Slots.ShouldBeEmpty();
        days[1].Slots.ShouldNotBeEmpty();
    }

    [Fact]
    public void AClosureBeatsTheWeeklyRules()
    {
        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "18:00")],
            [AvailabilityOverride.Closure(Monday, "Bank holiday")],
            [],
            Request()
        );

        days.Single().Slots.ShouldBeEmpty();
    }

    [Fact]
    public void AnExtraordinaryOpeningReplacesTheWeeklyRulesRatherThanAddingToThem()
    {
        var sunday = Monday.AddDays(-1);

        var days = AvailabilityCalculator.Calculate(
            sunday,
            sunday,
            // A Sunday rule exists, and is deliberately ignored: "open 14–16
            // that Sunday" means those hours and no others.
            [Rule(DayOfWeek.Sunday, "09:00", "12:00")],
            [AvailabilityOverride.Opening(sunday, new TimeOnly(14, 0), new TimeOnly(16, 0), "Event")],
            [],
            Request(durationMinutes: 60, granularity: 60)
        );

        LocalStarts(days).ShouldBe(["14:00", "15:00"]);
    }

    [Fact]
    public void RemovesTheSlotsAConfirmedAppointmentOccupies()
    {
        var busyStart = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "13:00")],
            [],
            // 09:00 UTC is 10:00 in Rome.
            [new BusyInterval(busyStart, busyStart.AddHours(1))],
            Request(durationMinutes: 60, granularity: 60)
        );

        LocalStarts(days).ShouldBe(["09:00", "11:00", "12:00"]);
    }

    [Fact]
    public void BackToBackIsAllowedWithoutABuffer()
    {
        // Half-open intervals: a slot ending exactly when another starts does
        // not overlap it.
        var busyStart = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "12:00")],
            [],
            [new BusyInterval(busyStart, busyStart.AddHours(1))],
            Request(durationMinutes: 60, granularity: 60, buffer: 0)
        );

        // Busy is 09:00–10:00 local; 10:00 is free.
        LocalStarts(days).ShouldBe(["10:00", "11:00"]);
    }

    [Fact]
    public void TheBufferKeepsTheSlotsOnBothSidesOfABookingFree()
    {
        var busyStart = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "14:00")],
            [],
            // 10:00–11:00 local.
            [new BusyInterval(busyStart, busyStart.AddHours(1))],
            Request(durationMinutes: 60, granularity: 60, buffer: 15)
        );

        // 09:00 would end at 10:00, which is now too close; 11:00 would start
        // at 11:00, likewise. The gap is what the buffer is for.
        LocalStarts(days).ShouldBe(["12:00", "13:00"]);
    }

    [Fact]
    public void RefusesSlotsThatAreTooSoon()
    {
        // 08:00 UTC on the Monday itself is 09:00 in Rome.
        var nowUtc = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "16:00")],
            [],
            [],
            Request(durationMinutes: 60, granularity: 60, noticeHours: 2, nowUtc: nowUtc)
        );

        // Nobody books for ten minutes from now: the first offered hour is
        // two hours out.
        LocalStarts(days)[0].ShouldBe("11:00");
    }

    [Fact]
    public void RefusesSlotsBeyondTheHorizon()
    {
        var nowUtc = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
        var farAway = Monday.AddDays(10);

        var days = AvailabilityCalculator.Calculate(
            farAway,
            farAway,
            [Rule(farAway.DayOfWeek, "09:00", "12:00")],
            [],
            [],
            Request(nowUtc: nowUtc, maxAdvanceDays: 5)
        );

        days.Single().Slots.ShouldBeEmpty();
    }

    [Fact]
    public void AFullDayOffersNothing()
    {
        var busyStart = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "09:00", "11:00")],
            [],
            [new BusyInterval(busyStart, busyStart.AddHours(2))],
            Request(durationMinutes: 60, granularity: 60)
        );

        days.Single().Slots.ShouldBeEmpty();
    }

    [Fact]
    public void SkipsTheHourThatDoesNotExistWhenTheClocksGoForward()
    {
        // Rome springs forward on 29 March 2026: 02:00 becomes 03:00, so
        // 02:00–02:59 simply does not happen.
        var springForward = new DateOnly(2026, 3, 29);

        var days = AvailabilityCalculator.Calculate(
            springForward,
            springForward,
            [Rule(DayOfWeek.Sunday, "00:00", "06:00")],
            [],
            [],
            Request(durationMinutes: 60, granularity: 60)
        );

        var starts = LocalStarts(days);

        // Nobody can turn up at 02:00 that day, so it is not offered.
        starts.ShouldNotContain("02:00");
        starts.ShouldContain("01:00");
        starts.ShouldContain("03:00");
    }

    [Fact]
    public void TheShortDayHasOneSlotFewerThanTheDayBefore()
    {
        var springForward = new DateOnly(2026, 3, 29);
        var normalSunday = springForward.AddDays(-7);
        var rules = new[] { Rule(DayOfWeek.Sunday, "00:00", "06:00") };

        var shortDay = AvailabilityCalculator.Calculate(
            springForward,
            springForward,
            rules,
            [],
            [],
            Request(durationMinutes: 60, granularity: 60)
        );

        var normalDay = AvailabilityCalculator.Calculate(
            normalSunday,
            normalSunday,
            rules,
            [],
            [],
            Request(durationMinutes: 60, granularity: 60)
        );

        // 23 hours instead of 24. Walking the grid in UTC would have produced
        // the same count on both days, and every slot shifted by an hour for
        // half the year.
        shortDay.Single().Slots.Count.ShouldBe(normalDay.Single().Slots.Count - 1);
    }

    [Fact]
    public void OffersTheRepeatedHourOnceWhenTheClocksGoBack()
    {
        // Rome falls back on 25 October 2026: 03:00 returns to 02:00, so the
        // 02:00 hour happens twice.
        var fallBack = new DateOnly(2026, 10, 25);

        var days = AvailabilityCalculator.Calculate(
            fallBack,
            fallBack,
            [Rule(DayOfWeek.Sunday, "00:00", "06:00")],
            [],
            [],
            Request(durationMinutes: 60, granularity: 60)
        );

        var slots = days.Single().Slots;

        // Offered once, deterministically: two identical-looking buttons that
        // book different instants is worse than one.
        slots.Count(slot => TimeZoneInfo.ConvertTimeFromUtc(slot.StartUtc, Rome).Hour == 2)
            .ShouldBe(1);

        // Every offered instant is distinct — the real guarantee behind the
        // count above.
        slots.Select(slot => slot.StartUtc).Distinct().Count().ShouldBe(slots.Count);
    }

    [Fact]
    public void TwoWindowsOnOneDayComeBackInOrder()
    {
        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "15:00", "17:00"), Rule(DayOfWeek.Monday, "09:00", "11:00")],
            [],
            [],
            Request(durationMinutes: 60, granularity: 60)
        );

        // The rules arrived afternoon-first; the answer must not.
        LocalStarts(days).ShouldBe(["09:00", "10:00", "15:00", "16:00"]);
    }

    [Fact]
    public void ADayWithNoRuleAtAllOffersNothing()
    {
        var days = AvailabilityCalculator.Calculate(Monday, Monday, [], [], [], Request());

        days.Single().Slots.ShouldBeEmpty();
    }

    [Fact]
    public void AnInvertedRuleIsIgnoredRatherThanLooping()
    {
        var days = AvailabilityCalculator.Calculate(
            Monday,
            Monday,
            [Rule(DayOfWeek.Monday, "18:00", "09:00")],
            [],
            [],
            Request()
        );

        // Nonsense in configuration must produce no slots, not an infinite
        // walk or a day that wraps into the next.
        days.Single().Slots.ShouldBeEmpty();
    }

    [Fact]
    public void AnEmptyRangeAsksNothingOfTheDatabaseOrTheClock()
    {
        AvailabilityCalculator
            .Calculate(Monday, Monday.AddDays(-1), [], [], [], Request())
            .ShouldBeEmpty();
    }

    [Fact]
    public void AServiceWithNoDurationOffersNothing()
    {
        AvailabilityCalculator
            .Calculate(
                Monday,
                Monday,
                [Rule(DayOfWeek.Monday, "09:00", "18:00")],
                [],
                [],
                Request(durationMinutes: 0)
            )
            .ShouldBeEmpty();
    }
}

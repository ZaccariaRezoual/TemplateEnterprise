using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnterpriseFramework.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.IntegrationTests;

/// <summary>
/// The Appointments module against a real PostgreSQL.
///
/// Three of these tests carry the module's whole design, and none of them is
/// visible from the interface:
/// - two simultaneous confirmations on overlapping requests must leave
///   exactly one appointment standing;
/// - confirming one must clear the requests it makes impossible;
/// - the anonymous availability endpoint must never say WHO booked an hour.
///
/// The first is the reason the exclusion constraint exists at all, and it
/// cannot be written without a real database: an in-memory provider has no
/// exclusion constraints, so the bug it guards against would pass every test.
/// </summary>
public sealed class AppointmentsModuleTests : IClassFixture<PostgresApiFactory>
{
    private const string Password = "Str0ngPassphrase";

    private readonly PostgresApiFactory _factory;

    public AppointmentsModuleTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// A host with no minimum notice and a wide horizon, so the tests can book
    /// tomorrow without fighting the defaults.
    /// </summary>
    private WebApplicationFactory<Program> Host =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Modules:Appointments:MinimumNoticeHours", "0");
            builder.UseSetting("Modules:Appointments:TimeZone", "UTC");
            builder.UseSetting("Modules:Appointments:SlotGranularityMinutes", "30");
        });

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    /// <summary>Tomorrow, so nothing collides with "now" and the notice is moot.</summary>
    private static DateOnly Tomorrow => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    private static string Iso(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public async Task TwoSimultaneousConfirmationsOnOverlappingRequests_LeaveExactlyOneStanding()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);

        // Two different customers ask for the very same hour. Both are
        // allowed to: a request reserves nothing.
        var first = await BookAsync(serviceId, slot);
        var second = await BookAsync(serviceId, slot);

        // Fired together, deliberately: the failure this guards against only
        // happens when the two overlap in time.
        var confirmations = await Task.WhenAll(
            ConfirmAsync(admin, first),
            ConfirmAsync(admin, second)
        );

        var succeeded = confirmations.Count(response => response.IsSuccessStatusCode);
        succeeded.ShouldBe(1);

        var loser = confirmations.Single(response => !response.IsSuccessStatusCode);
        // A conflict, not a five-hundred: two administrators confirming at
        // the same instant is a legitimate thing to do, and the loser needs a
        // sentence they can act on.
        loser.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConfirmingARequest_CancelsTheOnesItMakesImpossible()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);
        var winner = await BookAsync(serviceId, slot);
        var (loserId, loserClient) = await BookWithClientAsync(serviceId, slot);

        (await ConfirmAsync(admin, winner)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var mine = await loserClient.GetAsync(
            new Uri("/api/appointments/mine?includePast=true", UriKind.Relative)
        );
        using var body = await ReadJson(mine);

        var displaced = body
            .RootElement.EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == loserId);

        // Leaving it pending would be a person waiting for an answer that
        // will never come.
        displaced.GetProperty("status").GetString().ShouldBe("Cancelled");
        displaced.GetProperty("cancellationReason").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AvailabilityIsAnonymousAndNeverSaysWhoBookedAnHour()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);
        var booked = await BookAsync(serviceId, slot);
        (await ConfirmAsync(admin, booked)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var anonymous = Host.CreateClient();
        var response = await anonymous.GetAsync(AvailabilityUri(serviceId));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();

        // The taken hour is simply gone from the list...
        using var body = JsonDocument.Parse(payload);
        AllSlots(body).ShouldNotContain(slot);

        // ...and nothing about the person who took it is anywhere in the
        // answer. That is the difference between a public calendar and a
        // data leak.
        payload.ShouldNotContain("customer", Case.Insensitive);
        payload.ShouldNotContain("@example.com", Case.Insensitive);
    }

    [Fact]
    public async Task ABookedSlotIsNoLongerOffered()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);
        var appointmentId = await BookAsync(serviceId, slot);

        // Still offered: a request reserves nothing.
        (await SlotsAsync(serviceId)).ShouldContain(slot);

        (await ConfirmAsync(admin, appointmentId)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await SlotsAsync(serviceId)).ShouldNotContain(slot);
    }

    [Fact]
    public async Task BookingAnHourThatIsNotOffered_IsRefused()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var (customer, _) = await RegisterAsync();

        // 03:17 is not on the grid, and the server recomputes rather than
        // trusting whatever the browser sent.
        var offGrid = Tomorrow.ToDateTime(new TimeOnly(3, 17));

        var response = await customer.PostAsync(
            new Uri("/api/appointments", UriKind.Relative),
            Json(
                new
                {
                    serviceId,
                    startUtc = DateTime.SpecifyKind(offGrid, DateTimeKind.Utc),
                    contactPhone = "+39 333 1234567",
                }
            )
        );

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task BookingWithoutAPhoneNumber_IsRefused()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);
        var (customer, _) = await RegisterAsync();

        var response = await customer.PostAsync(
            new Uri("/api/appointments", UriKind.Relative),
            Json(
                new
                {
                    serviceId,
                    startUtc = slot,
                    contactPhone = "",
                }
            )
        );

        // A booking with no way to reach the customer is the one that costs
        // the business the slot when something changes.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ACustomerCannotSeeOrCancelSomeoneElsesAppointment()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);
        var (ownerId, _) = await BookWithClientAsync(serviceId, slot);

        var (stranger, _) = await RegisterAsync();

        var mine = await stranger.GetAsync(new Uri("/api/appointments/mine", UriKind.Relative));
        using var body = await ReadJson(mine);
        body.RootElement.GetArrayLength().ShouldBe(0);

        var cancel = await stranger.PostAsync(
            new Uri($"/api/appointments/{ownerId}/cancel", UriKind.Relative),
            Json(new { reason = "Not mine" })
        );

        // 404, not 403: a different answer would let anyone probe which
        // identifiers exist.
        cancel.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TheCalendarRefusesACallerWithoutThePermission()
    {
        var (customer, _) = await RegisterAsync();

        var response = await customer.GetAsync(
            new Uri(
                $"/api/admin/appointments?from={DateTime.UtcNow:O}&to={DateTime.UtcNow.AddDays(1):O}",
                UriKind.Relative
            )
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MovingAnAppointmentOntoAnOccupiedHour_IsRefusedByTheServer()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slots = await SlotsAsync(serviceId);
        var first = slots[0];
        var second = slots.First(slot => slot >= first.AddHours(1));

        var occupied = await BookAsync(serviceId, first);
        (await ConfirmAsync(admin, occupied)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var mover = await BookAsync(serviceId, second);
        (await ConfirmAsync(admin, mover)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await admin.PostAsync(
            new Uri($"/api/admin/appointments/{mover}/reschedule", UriKind.Relative),
            Json(new { startUtc = first })
        );

        // A drag in the browser is an intention, not an authorization.
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task TheCalendarFileIsServedToItsOwnerAndToNobodyElse()
    {
        var admin = await AdminClientAsync();
        var serviceId = await PublishBookableServiceAsync(admin);
        await OpenEveryDayAsync(admin);

        var slot = await FirstSlotAsync(serviceId);
        var (appointmentId, owner) = await BookWithClientAsync(serviceId, slot);

        var uri = new Uri($"/api/appointments/{appointmentId}/calendar.ics", UriKind.Relative);

        var mine = await owner.GetAsync(uri);
        mine.StatusCode.ShouldBe(HttpStatusCode.OK);
        mine.Content.Headers.ContentType?.MediaType.ShouldBe("text/calendar");

        var ics = await mine.Content.ReadAsStringAsync();
        ics.ShouldContain("BEGIN:VCALENDAR");
        // The stable identity is what lets a later download REPLACE the entry
        // in the customer's calendar instead of adding a second one.
        ics.ShouldContain($"UID:{appointmentId}@");
        ics.ShouldContain("SEQUENCE:");

        // The administration may read it too — it is on their calendar.
        (await admin.GetAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // A stranger gets 404, not 403: a different answer would let anyone
        // probe which identifiers exist.
        var (stranger, _) = await RegisterAsync();
        (await stranger.GetAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // And an anonymous caller never reaches it at all.
        var anonymous = Host.CreateClient();
        (await anonymous.GetAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AReminderClaimedTwice_IsOnlyEverSentOnce()
    {
        // The guarantee is a unique index, not a code check, and this is the
        // test that proves it: two sweeps racing — two API replicas during a
        // rolling deploy — must not put the same email in the customer's
        // inbox twice.
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<Modules.Appointments.Persistence.AppointmentsDbContext>();

        var appointmentId = Guid.NewGuid();

        dbContext.Reminders.Add(
            Modules.Appointments.Domain.AppointmentReminder.Claim(
                appointmentId,
                Modules.Appointments.Contracts.Events.ReminderKind.DayBefore
            )
        );
        await dbContext.SaveChangesAsync();

        dbContext.Reminders.Add(
            Modules.Appointments.Domain.AppointmentReminder.Claim(
                appointmentId,
                Modules.Appointments.Contracts.Events.ReminderKind.DayBefore
            )
        );

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

        dbContext.ChangeTracker.Clear();

        // The OTHER reminder is a different fact and is still allowed: the
        // constraint is on the pair, not on the appointment.
        dbContext.Reminders.Add(
            Modules.Appointments.Domain.AppointmentReminder.Claim(
                appointmentId,
                Modules.Appointments.Contracts.Events.ReminderKind.SameDayMorning
            )
        );
        await dbContext.SaveChangesAsync();

        var claimed = await dbContext
            .Reminders.AsNoTracking()
            .CountAsync(reminder => reminder.AppointmentId == appointmentId);
        claimed.ShouldBe(2);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static Uri AvailabilityUri(Guid serviceId) =>
        new(
            $"/api/appointments/availability?serviceId={serviceId}&from={Iso(Tomorrow)}&to={Iso(Tomorrow)}",
            UriKind.Relative
        );

    private static IReadOnlyList<DateTime> AllSlots(JsonDocument body) =>
        [
            .. body
                .RootElement.GetProperty("days")
                .EnumerateArray()
                .SelectMany(day => day.GetProperty("slots").EnumerateArray())
                .Select(slot => slot.GetProperty("startUtc").GetDateTime()),
        ];

    private async Task<IReadOnlyList<DateTime>> SlotsAsync(Guid serviceId)
    {
        var anonymous = Host.CreateClient();
        var response = await anonymous.GetAsync(AvailabilityUri(serviceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        return AllSlots(body);
    }

    private async Task<DateTime> FirstSlotAsync(Guid serviceId)
    {
        var slots = await SlotsAsync(serviceId);
        slots.ShouldNotBeEmpty();
        return slots[0];
    }

    /// <summary>Publishes a 60-minute bookable service through the Services API.</summary>
    private static async Task<Guid> PublishBookableServiceAsync(HttpClient admin)
    {
        var response = await admin.PostAsync(
            new Uri("/api/admin/services", UriKind.Relative),
            Json(
                new
                {
                    title = $"Consulenza {Guid.NewGuid():N}",
                    shortDescription = "Una riga.",
                    description = "Testo.",
                    durationMinutes = 60,
                    isPublished = true,
                    isBookable = true,
                    sortOrder = 0,
                }
            )
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    /// <summary>Opens every day of the week, so no test depends on a weekday.</summary>
    private static async Task OpenEveryDayAsync(HttpClient admin)
    {
        var rules = Enum.GetValues<DayOfWeek>()
            .Select(day => new
            {
                dayOfWeek = day.ToString(),
                startLocal = "08:00",
                endLocal = "20:00",
            })
            .ToArray();

        var response = await admin.PutAsync(
            new Uri("/api/admin/appointments/availability", UriKind.Relative),
            Json(new { rules, exceptions = Array.Empty<object>() })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<Guid> BookAsync(Guid serviceId, DateTime startUtc)
    {
        var (id, _) = await BookWithClientAsync(serviceId, startUtc);
        return id;
    }

    /// <summary>Registers a fresh customer and books the slot as them.</summary>
    private async Task<(Guid AppointmentId, HttpClient Client)> BookWithClientAsync(
        Guid serviceId,
        DateTime startUtc
    )
    {
        var (client, _) = await RegisterAsync();

        var response = await client.PostAsync(
            new Uri("/api/appointments", UriKind.Relative),
            Json(
                new
                {
                    serviceId,
                    startUtc,
                    contactPhone = "+39 333 1234567",
                    customerNote = "Arrivo con la macchina.",
                }
            )
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        // A request, never a confirmation: the public flow must not promise
        // what nobody has accepted.
        body.RootElement.GetProperty("status").GetString().ShouldBe("Requested");

        return (body.RootElement.GetProperty("id").GetGuid(), client);
    }

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient admin, Guid appointmentId) =>
        admin.PostAsync(
            new Uri($"/api/admin/appointments/{appointmentId}/confirm", UriKind.Relative),
            content: null
        );

    private async Task<(HttpClient Client, Guid UserId)> RegisterAsync()
    {
        var client = Host.CreateClient();
        var email = $"appt-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Appointment Tester", password = Password })
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var body = await ReadJson(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString()
        );

        return (client, body.RootElement.GetProperty("user").GetProperty("id").GetGuid());
    }

    /// <summary>
    /// Registers an account, promotes it to Admin and signs in again so the
    /// new permissions are present in the token.
    /// </summary>
    private async Task<HttpClient> AdminClientAsync()
    {
        var client = Host.CreateClient();
        var email = $"appt-admin-{Guid.NewGuid():N}@example.com";

        var registered = await client.PostAsync(
            new Uri("/api/auth/register", UriKind.Relative),
            Json(new { email, displayName = "Appointment Admin", password = Password })
        );
        registered.StatusCode.ShouldBe(HttpStatusCode.OK);

        Guid userId;
        using (var body = await ReadJson(registered))
        {
            userId = body.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<Modules.Authorization.Persistence.AuthorizationDbContext>();
            var adminRole = dbContext.Roles.Single(role =>
                role.Name == Modules.Authorization.Domain.BuiltInRoles.Admin
            );
            dbContext.UserRoles.Add(
                Modules.Authorization.Domain.UserRole.Grant(userId, adminRole.Id)
            );
            await dbContext.SaveChangesAsync();
        }

        var login = await client.PostAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            Json(new { email, password = Password })
        );
        using var loginBody = await ReadJson(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginBody.RootElement.GetProperty("accessToken").GetString()
        );

        return client;
    }
}

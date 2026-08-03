# Appointments Module

Public booking on real slots, an administrative calendar, and the reminders
around both.

It depends on [Services](../services/README.md): what gets booked is a
service, and its `DurationMinutes` is what sizes a slot. A service with no
duration cannot be booked, by construction.

## The three decisions everything else follows from

### 1. A request reserves nothing

`POST /api/appointments` produces a **`Requested`** appointment. Several
people may ask for the same hour, and whoever administers chooses. Only a
confirmation occupies the slot.

That is why the public flow says "request sent", never "booking confirmed" —
and why nothing needs an expiry: no slot is ever held hostage by a request
nobody answered.

The cost is that the conflict moves to confirmation time, so three things are
designed around it:

- **The database refuses double booking**, with an exclusion constraint on
  confirmed appointments only. A "is anything booked then?" check followed by
  an `INSERT` does not work: between the read and the write there is a gap, and
  two requests fit through it comfortably.
- **Confirming one cancels the ones it makes impossible**, with a reason the
  customer reads. Leaving them pending is people waiting for an answer that
  will never come.
- **The calendar flags a doomed request** (`hasConflict`) rather than letting
  an operator discover it by clicking confirm and being refused.

Confirmations are serialized by an advisory lock. Two of them touch each
other's rows in opposite orders, which is the textbook recipe for a deadlock;
serializing removes the cycle instead of racing it, and confirming is a person
clicking a button a few times an hour.

### 2. The business has one time zone

`Modules:Appointments:TimeZone`. Opening hours are **local to it** — "we open
at 9" means 9 there — while every instant is stored in UTC. The conversion
goes through `TimeZoneInfo`, never through arithmetic on a `DateTime`.

The clock-change days fall out of that on their own: the slot grid is walked
in local wall-clock time, so the hour that does not exist in March is skipped
and the one that happens twice in October is offered once. Both are covered by
unit tests, and both would be silently wrong if the grid were walked in UTC.

The browser's time zone is used to DISPLAY an hour, never to compute one.

### 3. Availability is a pure function

`AvailabilityCalculator` takes rules, exceptions, busy intervals, the
parameters and **the current instant**, and returns slots. No database, no
HTTP, no clock of its own.

That is not tidiness. The cases that matter here — the day the clocks change,
the day already full, the closure landing on an opening, the slot that is free
but too soon to book — can only be tested at all if the inputs are arguments.

## Endpoints

| Method | Route                                     | Who                                 |
| ------ | ----------------------------------------- | ----------------------------------- |
| GET    | `/api/appointments/availability`          | **anonymous**                       |
| POST   | `/api/appointments`                       | signed in                           |
| GET    | `/api/appointments/mine`                  | signed in                           |
| GET    | `/api/appointments/{id}/calendar.ics`     | proprietario, o `appointments.read` |
| POST   | `/api/appointments/{id}/cancel`           | signed in, owner                    |
| GET    | `/api/admin/appointments`                 | `appointments.read`                 |
| POST   | `/api/admin/appointments/{id}/confirm`    | `appointments.write`                |
| POST   | `/api/admin/appointments/{id}/reschedule` | `appointments.write`                |
| POST   | `/api/admin/appointments/{id}/cancel`     | `appointments.write`                |
| GET    | `/api/admin/appointments/availability`    | `appointments.read`                 |
| PUT    | `/api/admin/appointments/availability`    | `appointments.write`                |

**Availability is anonymous by necessity**: the visitor picks a time before
being asked to sign in, because asking first is what makes people give up. It
is safe to expose because the answer is instants and nothing else — it can say
an hour is taken, never by whom. There is a test asserting exactly that.

The customer's own endpoints take **no user identifier**: the caller comes
from the token, so there is nothing to tamper with. Asking for somebody else's
appointment answers 404, not 403 — a different answer would let anyone probe
which identifiers exist.

Rescheduling is **always revalidated server-side**. A drag in the calendar is
an intention, not an authorization, and the state it was dragged against is
minutes old.

## Public events

In `modules/appointments/shared/Events/`. Every one carries an
`AppointmentSummary` with the customer's address, the service title and the
business time zone — so a subscriber can write a complete message without
querying this module.

| Event                    | Raised when                        |
| ------------------------ | ---------------------------------- |
| `AppointmentRequested`   | a customer asks                    |
| `AppointmentConfirmed`   | an administrator accepts           |
| `AppointmentRescheduled` | a confirmed appointment moves      |
| `AppointmentCancelled`   | either side calls it off           |
| `AppointmentReminderDue` | one of the two reminders comes due |

Subscribers today: the Email module (messages, with the calendar file), and
this module's own bridge to Notifications.

## Reminders

Two, and they are modelled **differently because they are different things**:

1. `ReminderDayBeforeHours` — a distance from the appointment.
2. `ReminderSameDayLocalTime` — an **hour of the day**, 08:00 by default.

The second is not an offset, and that is the point: an appointment at 09:30
and one at 18:00 must both be reminded at 08:00. Expressed as "N hours before"
they would fire at 08:30 and 17:00, which is correct for exactly one
appointment a day.

A `BackgroundService` evaluates both every minute — the framework ships no
scheduler by design, so the module brings the smallest one that does the job.

**Idempotency is a unique index, not a code check.** Claiming a reminder means
inserting a row on `(appointment, kind)`; whoever inserts first publishes, and
a second instance — two replicas during a rolling deploy — fails to insert and
sends nothing. The row is written BEFORE the event: crash in between and a
customer misses one reminder, the other way round they get one per crash.

Only **confirmed** appointments earn reminders. Telling someone about an
appointment nobody has accepted is worse than silence.

## Calendar files

Confirmation, reschedule and cancellation carry an `.ics` attachment, and the
confirmation email also links to Google Calendar with the details pre-filled.
No OAuth, no tokens to refresh, no consent screen.

The part that earns its keep is `SEQUENCE`: an update carries the **same
`UID`** with a higher sequence, so the customer's calendar MOVES the existing
entry instead of accumulating duplicates.

Two-way synchronisation is deliberately out of scope — it needs OAuth and a
consent almost nobody grants a supplier, for a case the `.ics` already covers.

## Frontend

`@enterprise/module-appointments`. The host wires it once:

```ts
installAppointmentsModule({
  api,
  session: {
    isAuthenticated: () => session.isAuthenticated,
    signInPath: () => `/login?redirect=${encodeURIComponent(currentPath)}`,
  },
});
```

The `session` seam is asked of the HOST rather than read from the Auth
module — the same reason the public site asks — so this module stays
removable.

**The booking flow is three screens**: day, hour, confirm. One page holding a
month calendar, a list of hours and a sign-in box is unreadable at 375px, and
booking is something people do from a phone. The account is asked for on the
**last** step, and the chosen slot survives the sign-in because it lives in
the page's state.

The slot can disappear while the form is being filled in. The page handles it
explicitly: back to the hours with an explanation, not a generic error on
submit.

### The calendar

`AppointmentCalendar.vue` is **the only file that knows FullCalendar exists**.
Everything above it speaks in appointments and dates, so replacing the library
is one file rather than a rewrite.

It is themed from our tokens: the library ships its own stylesheet, which is
exactly how literal values climb back in after the design system has kept them
out. The `<style>` block binds FullCalendar's CSS variables to our semantic
ones, so the calendar follows light and dark like everything else.

Only the free views are used — month, week and day. The resource/timeline
views are the paid ones and nothing here reaches for them.

## Configuration

```json
{
  "Modules": {
    "Appointments": {
      "TimeZone": "Europe/Rome",
      "SlotGranularityMinutes": 15,
      "BufferMinutes": 0,
      "MinimumNoticeHours": 2,
      "MaxAdvanceDays": 60,
      "ReminderDayBeforeHours": 24,
      "ReminderSameDayLocalTime": "08:00",
      "CustomerCancellationCutoffHours": 24
    }
  }
}
```

Defaults are chosen to be sane rather than permissive: a misconfigured
deployment should offer too few slots, never too many.

## Not multi-tenant

It does not implement `ITenantOwned`, for the same reason as Services: the
availability endpoint is **anonymous**, and an anonymous request has no tenant
to scope by. A multi-tenant deployment needs to resolve the tenant from the
host name first, which is a host concern.

## What this module does not do

- **Payments** — another domain, with its own legal requirements.
- **Several operators or rooms.** The exclusion constraint is already written
  to accept one: the day it arrives, the operator column joins the constraint
  `WITH =` (and `btree_gist` becomes necessary, which it currently is not).
  The rest — who is assigned, who sees what — is a plan of its own.
- **Waiting lists and recurring appointments** — added when somebody asks.

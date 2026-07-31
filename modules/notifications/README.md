# Notifications Module

The in-app notification centre.

## How other modules notify a user

Publish the public contract event — no reference to this module required:

```csharp
await eventBus.PublishAsync(new NotificationRequested(userId, "Report ready", "Your export finished."));
```

That keeps notifying a fire-and-forget concern: the publisher does not care
whether delivery is a bell in the UI today and a push message tomorrow.

## Persist first, push later

Notifications are stored before anything else happens with them. That is what
lets the Realtime module (Fase 6) simply push an already-stored record: a
notification the user was offline for is still waiting in the centre when they
return. This module needs no changes when realtime arrives.

## Scoping

Endpoints need no permission because they are inherently scoped to the caller
— a user may always read and dismiss their own notifications. The user id is
part of the query FILTER, not just the lookup, so reading or marking someone
else's notification is impossible rather than merely unlikely.

| Method | Route                     |
| ------ | ------------------------- |
| GET    | `/api/notifications`      |
| POST   | `/api/notifications/read` |

## Frontend (`@enterprise/module-notifications`)

Exports `NotificationBell` and `useNotificationsStore`. The store is Pinia,
not a query cache: the unread badge must not flicker mid-interaction because
of a background refetch. `markAsRead` updates local state first so the badge
responds immediately.

Realtime pushes will call `receive()`, which ignores duplicates — a push can
race with a refetch.

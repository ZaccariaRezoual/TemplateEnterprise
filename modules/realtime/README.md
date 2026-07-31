# Realtime Module

Delivers domain events to connected clients over SignalR.

## How a feature becomes live

Implement `IRealtimeEvent` (declared in `Application/Abstractions`) on a domain
event. That is the entire integration:

```csharp
public sealed record OrderShipped(Guid OrderId, Guid CustomerId) : IRealtimeEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public RealtimeAudience Audience => RealtimeAudience.ForUser(CustomerId);
    public string Channel => "order.shipped";
}
```

Publish it on the event bus as usual. The module has **no reference to
Realtime and no idea SignalR exists** — it declares what happened and who
cares. The bridge picks up every `IRealtimeEvent` implementation found at
startup, so no registration is needed either.

On the client:

```ts
useRealtimeEvent<IOrderShipped>("order.shipped", (order) => { … });
useRealtimeInvalidation("order.shipped", ["orders"]);   // refetch instead
```

`useRealtimeInvalidation` is usually what you want: invalidating rather than
patching the cache keeps the server authoritative, so a refetch returns what
the user is actually allowed to see.

## Design decisions

**One hub, not one per feature.** PLAN.md sketched `NotificationHub` and
`PresenceHub`; a single `RealtimeHub` replaced them because every extra hub
costs another connection and another auth path, while a channel name already
separates content. Clients subscribe to channels, not to hubs.

**Audiences are resolved server-side.** There is no client method to join a
group — a client that can name a group can read other users' traffic. Groups
(`user:{id}`, `role:{name}`) are assigned from the authenticated principal at
connection time.

**An empty audience reaches nobody**, never everybody. A misconfigured event
should be silently harmless, not a data leak.

**Thin hub.** The hub registers connections and joins groups; who receives
what lives in `RealtimeDispatcher`, which is directly unit-testable because it
needs a hub context rather than a whole connection.

**Delivery failure never fails the publisher.** The domain operation already
succeeded and was persisted; refusing it because a WebSocket was unavailable
would be worse than the client refreshing a moment later.

## Authentication

Browsers cannot set an `Authorization` header on a WebSocket handshake, so the
client passes the token as `?access_token=`. The host accepts a query token
**only for `/hubs` paths** — allowing it everywhere would put tokens in server
logs, browser history and `Referer` headers.

The token comes from a _function_, so a session that refreshed while the
network was down reconnects with the current token instead of being rejected.

## Scale-out

With `ConnectionStrings:Redis` configured, both the SignalR backplane and the
connection registry use Redis, and several API replicas behave as one. Without
it the module still works, but presence and delivery are per-instance —
fine for development, wrong for a scaled deployment.

Registry entries carry a TTL: a process killed mid-flight never runs its
disconnect handler, and without expiry those connections would look online
forever.

## Reconnection

The client uses an explicit exponential backoff with jitter and no give-up
point. SignalR's default stops after about a minute, which turns a lift ride
or a laptop suspend into a permanently dead connection recoverable only by
reloading the page. Jitter matters too: without it every client of a restarted
server reconnects in the same instant and knocks it over again.

Events are **not replayed** on reconnect. Anything that must survive a
disconnection has to be persisted and refetched — which is exactly why
Notifications stores before it pushes.

## Endpoints

| Path                     | Purpose                         |
| ------------------------ | ------------------------------- |
| `/hubs/realtime`         | The SignalR hub (authenticated) |
| `/api/realtime/presence` | Accounts currently connected    |

## Dev-server note

The Vite proxy needs `ws: true` on `/hubs`, otherwise the negotiate request is
proxied but the WebSocket upgrade is not, and the client silently degrades to
long polling — or fails outright.

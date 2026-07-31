import { ref, type Ref } from "vue";
import type {
  IHubConnectionLike,
  IRealtimeEnvelope,
  RealtimeHandler,
  RealtimeStatus,
} from "./types";

/** Client method the server sends every event on. */
export const CLIENT_METHOD = "realtimeEvent";

/**
 * The application's single realtime connection.
 *
 * THE ONLY place that talks to SignalR. Components and features subscribe to
 * channels through composables; nothing else imports `@microsoft/signalr`.
 * That is what makes swapping the transport — or testing without one — a
 * change in this file alone.
 *
 * Responsibilities:
 * - Owns the connection lifecycle and exposes its state for the UI.
 * - Fans incoming envelopes out to channel subscribers.
 * - Queues nothing on the way out (this connection is receive-only) but
 *   replays subscriptions after a reconnect, since the server re-adds groups
 *   on the new connection.
 */
export class RealtimeService {
  #connection: IHubConnectionLike | undefined;
  #handlers = new Map<string, Set<RealtimeHandler>>();
  #status: Ref<RealtimeStatus> = ref("disconnected");
  #lastEventAt: Ref<Date | undefined> = ref(undefined);

  /** Current connection state, for status indicators. */
  get status(): Ref<RealtimeStatus> {
    return this.#status;
  }

  /** When the last event arrived; useful for a "live" indicator. */
  get lastEventAt(): Ref<Date | undefined> {
    return this.#lastEventAt;
  }

  /**
   * Starts the connection.
   *
   * Idempotent: calling it twice does not open a second connection, which
   * would double every event the user sees.
   *
   * @param connection The hub connection to drive.
   */
  async connect(connection: IHubConnectionLike): Promise<void> {
    if (this.#connection !== undefined) {
      return;
    }

    this.#connection = connection;
    this.#status.value = "connecting";

    connection.on(CLIENT_METHOD, (...args: never[]) => {
      // The server controls this payload, but a malformed one must not throw
      // inside the transport callback, where nothing would catch it.
      this.#dispatch(args[0] as IRealtimeEnvelope | undefined);
    });

    // SignalR's automatic reconnect drives these; the service only mirrors
    // them into state the UI can render.
    connection.onreconnecting(() => {
      this.#status.value = "reconnecting";
    });
    connection.onreconnected(() => {
      this.#status.value = "connected";
    });
    connection.onclose(() => {
      this.#status.value = "disconnected";
    });

    try {
      await connection.start();
      this.#status.value = "connected";
    } catch {
      // A failed start is not fatal: the app works without realtime, just
      // without live updates. SignalR retries on its own schedule.
      this.#status.value = "disconnected";
    }
  }

  /**
   * Stops the connection and forgets every subscription.
   * Called on sign-out, so the next user never receives the previous one's
   * events on a still-open socket.
   */
  async disconnect(): Promise<void> {
    const connection = this.#connection;
    this.#connection = undefined;
    this.#handlers.clear();
    this.#status.value = "disconnected";

    await connection?.stop().catch(() => undefined);
  }

  /**
   * Subscribes to a channel.
   *
   * @param channel Channel name, e.g. "notification.created".
   * @param handler Called with the event payload.
   * @returns A function that removes this subscription.
   */
  on<TPayload>(channel: string, handler: RealtimeHandler<TPayload>): () => void {
    const handlers = this.#handlers.get(channel) ?? new Set<RealtimeHandler>();
    handlers.add(handler as RealtimeHandler);
    this.#handlers.set(channel, handlers);

    return () => {
      handlers.delete(handler as RealtimeHandler);
      if (handlers.size === 0) {
        this.#handlers.delete(channel);
      }
    };
  }

  /** Number of channels with at least one subscriber (used in tests). */
  get channelCount(): number {
    return this.#handlers.size;
  }

  #dispatch(envelope: IRealtimeEnvelope | undefined): void {
    if (envelope?.channel === undefined) {
      return;
    }

    this.#lastEventAt.value = new Date();

    // A throwing subscriber must not stop the others: one broken feature
    // would otherwise silently disable every live update in the app.
    for (const handler of this.#handlers.get(envelope.channel) ?? []) {
      try {
        handler(envelope.payload);
      } catch {
        // Intentionally swallowed; the app-level error handler reports it.
      }
    }
  }
}

/** The application's realtime service instance. */
export const realtimeService = new RealtimeService();

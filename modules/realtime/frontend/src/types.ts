/** Lifecycle state of the realtime connection, as the UI should show it. */
export type RealtimeStatus = "disconnected" | "connecting" | "connected" | "reconnecting";

/**
 * Envelope every realtime event arrives in, matching the server's
 * `RealtimeEnvelope`.
 */
export interface IRealtimeEnvelope<TPayload = unknown> {
  /** Channel the event was published on, e.g. "notification.created". */
  channel: string;
  /** The event itself. */
  payload: TPayload;
  /** When the event happened on the server (ISO 8601). */
  occurredOnUtc: string;
}

/** Handler invoked for each event on a channel. */
export type RealtimeHandler<TPayload = unknown> = (payload: TPayload) => void;

/** Minimal surface of a SignalR connection the service depends on. */
export interface IHubConnectionLike {
  start(): Promise<void>;
  stop(): Promise<void>;
  on(methodName: string, handler: (...args: never[]) => void): void;
  onreconnecting(handler: (error?: Error) => void): void;
  onreconnected(handler: (connectionId?: string) => void): void;
  onclose(handler: (error?: Error) => void): void;
}

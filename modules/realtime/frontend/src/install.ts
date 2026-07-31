import { HubConnectionBuilder, HttpTransportType, LogLevel } from "@microsoft/signalr";
import { realtimeService } from "./RealtimeService";

/** Path the hub is served on, matching the backend module. */
export const HUB_PATH = "/hubs/realtime";

/** Integration seams the HOST exposes and this module plugs into. */
export interface RealtimeModuleHost {
  /**
   * Returns the current access token, or a nullish value when signed out.
   *
   * A FUNCTION, not a value: SignalR calls it again on every reconnect, so a
   * session that refreshed its token while the network was down reconnects
   * with the new one instead of being rejected.
   */
  getAccessToken: () => string | null | undefined;
}

let host: RealtimeModuleHost | undefined;

/**
 * Wires the Realtime module into the host application. Call once at
 * bootstrap; connecting is a separate step, because there is nothing to
 * connect to until the user is signed in.
 *
 * @param moduleHost The host integration seams.
 */
export function installRealtimeModule(moduleHost: RealtimeModuleHost): void {
  host = moduleHost;
}

/**
 * Opens the realtime connection for the signed-in user.
 *
 * Reconnection uses an explicit backoff schedule rather than SignalR's
 * default: the default gives up after about a minute, which turns a lift
 * ride or a laptop suspend into a permanently dead connection with no way
 * back except a page reload.
 */
export async function connectRealtime(): Promise<void> {
  if (host === undefined) {
    throw new Error("Realtime module is not installed. Call installRealtimeModule() at bootstrap.");
  }

  const connection = new HubConnectionBuilder()
    .withUrl(HUB_PATH, {
      // The browser cannot set an Authorization header on a WebSocket
      // handshake; the server accepts this query token for hub paths only.
      accessTokenFactory: () => host?.getAccessToken() ?? "",
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (context) => {
        // Exponential with a ceiling and jitter: without jitter every client
        // of a restarted server reconnects in the same instant and knocks it
        // over again.
        const base = Math.min(1000 * 2 ** context.previousRetryCount, 30_000);
        return base + Math.random() * 1000;
      },
    })
    .configureLogging(LogLevel.Warning)
    .build();

  await realtimeService.connect(connection);
}

/** Closes the realtime connection (on sign-out). */
export async function disconnectRealtime(): Promise<void> {
  await realtimeService.disconnect();
}

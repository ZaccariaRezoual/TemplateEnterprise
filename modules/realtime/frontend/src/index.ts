/**
 * Public surface of `@enterprise/module-realtime`.
 *
 * Features use `useRealtimeEvent` or `useRealtimeInvalidation`; nothing
 * outside this package imports `@microsoft/signalr`.
 */
export { default as ConnectionIndicator } from "./components/ConnectionIndicator.vue";
export { useRealtime, useRealtimeEvent, useRealtimeInvalidation } from "./composables/useRealtime";
export { connectRealtime, disconnectRealtime, HUB_PATH, installRealtimeModule } from "./install";
export type { RealtimeModuleHost } from "./install";
export { realtimeService, RealtimeService, CLIENT_METHOD } from "./RealtimeService";
export type { IRealtimeEnvelope, RealtimeHandler, RealtimeStatus } from "./types";

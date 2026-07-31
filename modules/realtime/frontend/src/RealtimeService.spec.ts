import { describe, expect, it, vi } from "vitest";
import { RealtimeService } from "./RealtimeService";
import type { IHubConnectionLike, IRealtimeEnvelope } from "./types";

/**
 * Fake hub connection: the value of these tests is the lifecycle and dispatch
 * logic, not SignalR's transport, which has its own test suite.
 */
class FakeConnection implements IHubConnectionLike {
  startCalls = 0;
  stopCalls = 0;
  shouldFailStart = false;

  #handler?: (envelope: IRealtimeEnvelope) => void;
  #onReconnecting?: () => void;
  #onReconnected?: () => void;
  #onClose?: () => void;

  async start(): Promise<void> {
    this.startCalls += 1;
    if (this.shouldFailStart) {
      throw new Error("network down");
    }
  }

  async stop(): Promise<void> {
    this.stopCalls += 1;
  }

  on(_method: string, handler: (...args: never[]) => void): void {
    this.#handler = handler as unknown as (envelope: IRealtimeEnvelope) => void;
  }

  onreconnecting(handler: () => void): void {
    this.#onReconnecting = handler;
  }

  onreconnected(handler: () => void): void {
    this.#onReconnected = handler;
  }

  onclose(handler: () => void): void {
    this.#onClose = handler;
  }

  /** Simulates a server push. */
  emit(channel: string, payload: unknown): void {
    this.#handler?.({ channel, payload, occurredOnUtc: new Date().toISOString() });
  }

  dropConnection(): void {
    this.#onReconnecting?.();
  }

  restoreConnection(): void {
    this.#onReconnected?.();
  }

  close(): void {
    this.#onClose?.();
  }
}

describe("RealtimeService", () => {
  it("reports connected after a successful start", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();

    await service.connect(connection);

    expect(service.status.value).toBe("connected");
    expect(connection.startCalls).toBe(1);
  });

  it("does not open a second connection", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();

    await service.connect(connection);
    await service.connect(new FakeConnection());

    // A second connection would deliver every event twice.
    expect(connection.startCalls).toBe(1);
    expect(service.status.value).toBe("connected");
  });

  it("stays usable when the connection cannot be established", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    connection.shouldFailStart = true;

    await service.connect(connection);

    // The app works without live updates; it must not crash on startup.
    expect(service.status.value).toBe("disconnected");
  });

  it("delivers events to the matching channel only", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);

    const onNotification = vi.fn();
    const onOther = vi.fn();
    service.on("notification.created", onNotification);
    service.on("user.updated", onOther);

    connection.emit("notification.created", { id: "1" });

    expect(onNotification).toHaveBeenCalledWith({ id: "1" });
    expect(onOther).not.toHaveBeenCalled();
  });

  it("keeps other subscribers working when one throws", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);

    const healthy = vi.fn();
    service.on("channel", () => {
      throw new Error("broken feature");
    });
    service.on("channel", healthy);

    connection.emit("channel", {});

    // One broken feature must not silently disable live updates everywhere.
    expect(healthy).toHaveBeenCalledOnce();
  });

  it("stops delivering after unsubscribing", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);

    const handler = vi.fn();
    const unsubscribe = service.on("channel", handler);
    unsubscribe();

    connection.emit("channel", {});

    expect(handler).not.toHaveBeenCalled();
    expect(service.channelCount).toBe(0);
  });

  it("mirrors the reconnect lifecycle into status", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);

    connection.dropConnection();
    expect(service.status.value).toBe("reconnecting");

    connection.restoreConnection();
    expect(service.status.value).toBe("connected");

    connection.close();
    expect(service.status.value).toBe("disconnected");
  });

  it("forgets subscriptions on disconnect", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);
    const handler = vi.fn();
    service.on("channel", handler);

    await service.disconnect();

    // The next user must not inherit the previous one's subscriptions.
    expect(service.channelCount).toBe(0);
    expect(connection.stopCalls).toBe(1);
    expect(service.status.value).toBe("disconnected");
  });

  it("ignores a malformed envelope", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);
    const handler = vi.fn();
    service.on("channel", handler);

    connection.emit(undefined as unknown as string, {});

    expect(handler).not.toHaveBeenCalled();
  });

  it("records when the last event arrived", async () => {
    const service = new RealtimeService();
    const connection = new FakeConnection();
    await service.connect(connection);
    expect(service.lastEventAt.value).toBeUndefined();

    connection.emit("channel", {});

    expect(service.lastEventAt.value).toBeInstanceOf(Date);
  });
});

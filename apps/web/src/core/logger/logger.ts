import { env } from "@/core/config/env";

/** Severity levels, ordered from most to least verbose. */
export type LogLevel = "debug" | "info" | "warn" | "error";

/**
 * Sink that receives log entries. Implement it to forward logs elsewhere
 * (Sentry, an HTTP collector, a test spy) without touching call sites.
 */
export interface LogSink {
  /**
   * Writes one entry.
   *
   * @param level Severity of the entry.
   * @param message Human-readable message.
   * @param context Structured data attached to the entry.
   */
  write(level: LogLevel, message: string, context?: Record<string, unknown>): void;
}

const LEVEL_ORDER: Record<LogLevel | "silent", number> = {
  debug: 10,
  info: 20,
  warn: 30,
  error: 40,
  silent: 100,
};

/** Default sink writing to the browser console. */
export const consoleSink: LogSink = {
  write(level, message, context) {
    const method = level === "debug" ? "log" : level;
    if (context === undefined) {
      console[method](message);
    } else {
      console[method](message, context);
    }
  },
};

/**
 * Application logger.
 *
 * Responsibilities:
 * - Filters entries below the configured minimum level (`VITE_LOG_LEVEL`).
 * - Fans every entry out to the registered sinks.
 *
 * Features and components must log through this class, never through
 * `console` directly: that is what makes adding Sentry a one-line change in
 * the composition root instead of a codebase-wide edit.
 */
export class Logger {
  #sinks: LogSink[];
  #minimum: number;

  /**
   * @param sinks Sinks that receive every entry (defaults to the console sink).
   * @param level Minimum level to emit (defaults to `VITE_LOG_LEVEL`).
   */
  constructor(sinks: LogSink[] = [consoleSink], level: LogLevel | "silent" = env.VITE_LOG_LEVEL) {
    this.#sinks = sinks;
    this.#minimum = LEVEL_ORDER[level];
  }

  /**
   * Registers an additional sink at runtime (e.g. Sentry once initialized).
   *
   * @param sink The sink to add.
   */
  addSink(sink: LogSink): void {
    this.#sinks.push(sink);
  }

  /** Logs diagnostic detail useful only while debugging. */
  debug(message: string, context?: Record<string, unknown>): void {
    this.#write("debug", message, context);
  }

  /** Logs a normal, noteworthy application event. */
  info(message: string, context?: Record<string, unknown>): void {
    this.#write("info", message, context);
  }

  /** Logs a recoverable problem that deserves attention. */
  warn(message: string, context?: Record<string, unknown>): void {
    this.#write("warn", message, context);
  }

  /** Logs a failure. Pass the caught error in the context for stack traces. */
  error(message: string, context?: Record<string, unknown>): void {
    this.#write("error", message, context);
  }

  #write(level: LogLevel, message: string, context?: Record<string, unknown>): void {
    if (LEVEL_ORDER[level] < this.#minimum) {
      return;
    }
    for (const sink of this.#sinks) {
      sink.write(level, message, context);
    }
  }
}

/** Shared logger instance used across the application. */
export const logger = new Logger();

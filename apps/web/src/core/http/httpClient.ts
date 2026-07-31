import axios, { type AxiosInstance, type AxiosRequestConfig } from "axios";
import { env } from "@/core/config/env";
import { mapToApplicationError } from "@/core/http/errorMapper";
import { logger } from "@/core/logger/logger";

/** Options accepted by every HTTP method, minus what the client sets itself. */
export type RequestOptions = Omit<AxiosRequestConfig, "url" | "method" | "data" | "baseURL">;

/**
 * The single point in the application allowed to talk to Axios.
 *
 * Responsibilities:
 * - Owns the Axios instance and its configuration (base URL, timeout).
 * - Attaches the correlation id and, from Fase 4, the auth token.
 * - Converts every transport failure into an
 *   {@link import("@/core/errors/applicationError").ApplicationError}, so no
 *   caller ever sees an Axios error or an HTTP status code.
 *
 * Rule: features never import Axios and never instantiate this class. They call
 * a feature service, which calls the shared {@link httpClient} (and, from
 * Fase 3, the generated SDK configured on top of it). This is what makes
 * swapping the transport a change in one file.
 */
export class HttpClient {
  #instance: AxiosInstance;

  /**
   * @param instance Pre-configured Axios instance; a default one built from
   * the validated environment is used when omitted (tests inject their own).
   */
  constructor(instance: AxiosInstance = createDefaultInstance()) {
    this.#instance = instance;
    this.#registerInterceptors();
  }

  /**
   * Performs a GET request.
   *
   * @typeParam T Expected response payload.
   * @param url Path relative to the API base URL.
   * @param options Extra Axios options (params, signal, headers).
   * @returns The deserialized response payload.
   * @throws {ApplicationError} Any transport or API failure, already mapped.
   */
  async get<T>(url: string, options?: RequestOptions): Promise<T> {
    const response = await this.#instance.get<T>(url, options);
    return response.data;
  }

  /**
   * Performs a POST request.
   *
   * @typeParam T Expected response payload.
   * @param url Path relative to the API base URL.
   * @param body Request payload.
   * @param options Extra Axios options.
   * @returns The deserialized response payload.
   * @throws {ApplicationError} Any transport or API failure, already mapped.
   */
  async post<T>(url: string, body?: unknown, options?: RequestOptions): Promise<T> {
    const response = await this.#instance.post<T>(url, body, options);
    return response.data;
  }

  /**
   * Performs a PUT request.
   *
   * @typeParam T Expected response payload.
   * @param url Path relative to the API base URL.
   * @param body Request payload.
   * @param options Extra Axios options.
   * @returns The deserialized response payload.
   * @throws {ApplicationError} Any transport or API failure, already mapped.
   */
  async put<T>(url: string, body?: unknown, options?: RequestOptions): Promise<T> {
    const response = await this.#instance.put<T>(url, body, options);
    return response.data;
  }

  /**
   * Performs a PATCH request.
   *
   * @typeParam T Expected response payload.
   * @param url Path relative to the API base URL.
   * @param body Request payload.
   * @param options Extra Axios options.
   * @returns The deserialized response payload.
   * @throws {ApplicationError} Any transport or API failure, already mapped.
   */
  async patch<T>(url: string, body?: unknown, options?: RequestOptions): Promise<T> {
    const response = await this.#instance.patch<T>(url, body, options);
    return response.data;
  }

  /**
   * Performs a DELETE request.
   *
   * @typeParam T Expected response payload.
   * @param url Path relative to the API base URL.
   * @param options Extra Axios options.
   * @returns The deserialized response payload.
   * @throws {ApplicationError} Any transport or API failure, already mapped.
   */
  async delete<T>(url: string, options?: RequestOptions): Promise<T> {
    const response = await this.#instance.delete<T>(url, options);
    return response.data;
  }

  /**
   * The underlying Axios instance, exposed only so the generated SDK
   * (Fase 3) can be configured to run through this client. Application code
   * must not use it.
   */
  get instance(): AxiosInstance {
    return this.#instance;
  }

  #registerInterceptors(): void {
    this.#instance.interceptors.request.use((config) => {
      // Correlation id is generated client-side so a user action can be traced
      // across the browser console and the server logs (Seq) with one value.
      config.headers.set("X-Correlation-ID", crypto.randomUUID());
      return config;
    });

    this.#instance.interceptors.response.use(
      (response) => response,
      (error: unknown) => {
        const applicationError = mapToApplicationError(error);
        logger.error("HTTP request failed", {
          message: applicationError.message,
          type: applicationError.name,
          correlationId: applicationError.correlationId,
        });
        return Promise.reject(applicationError);
      },
    );
  }
}

function createDefaultInstance(): AxiosInstance {
  return axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: env.VITE_API_TIMEOUT_MS,
    headers: { Accept: "application/json" },
  });
}

/** Shared HTTP client used by every feature service. */
export const httpClient = new HttpClient();

import type { ApiClient } from "@enterprise/sdk";
import { executeSdkCall } from "@enterprise/shared";

/** A message written on the public contact form. */
export interface ContactMessage {
  /** Name the visitor typed. */
  name: string;
  /** Address to reply to. */
  email: string;
  /** The message itself. */
  body: string;
  /**
   * Honeypot. Always empty for a real visitor: the field is hidden from
   * people, and only automated submitters fill it. The server drops those
   * submissions and still answers with success.
   */
  website?: string | undefined;
}

let configuredApi: ApiClient | undefined;

/**
 * Injects the application's SDK client. Called by `installSiteModule` when
 * the host provides one.
 *
 * @param api The configured SDK client.
 */
export function provideSiteApi(api: ApiClient): void {
  configuredApi = api;
}

/**
 * Whether the contact form can submit.
 *
 * The public site also works without an API — a project may want only the
 * pages — so the form asks first and publishes the email address instead of
 * offering a form it could not send.
 *
 * @returns True when the host provided an SDK client.
 */
export function isContactAvailable(): boolean {
  return configuredApi !== undefined;
}

/**
 * Feature service of the Site module.
 */
export const siteApi = {
  /**
   * Submits the contact form.
   *
   * @param message The visitor's message.
   * @throws {import("@enterprise/shared").ValidationError} When the server rejects a field.
   * @throws {import("@enterprise/shared").ApplicationError} On any other failure, including
   * the 429 raised when the endpoint's rate limit is exceeded.
   */
  async sendContactMessage(message: ContactMessage): Promise<void> {
    if (configuredApi === undefined) {
      throw new Error("Site module has no API client: pass `api` to installSiteModule().");
    }

    await executeSdkCall(() =>
      configuredApi!.POST("/api/site/contact", {
        body: {
          name: message.name,
          email: message.email,
          body: message.body,
          website: message.website ?? null,
        },
      }),
    );
  },
};

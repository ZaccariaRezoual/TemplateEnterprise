/**
 * Contracts of the Demo feature, mirroring the backend module's response
 * records. From Fase 3 these types come from the generated SDK
 * (`packages/sdk`) instead of being hand-written here.
 */

/** Response of `GET /api/demo/ping`. */
export interface IPingResponse {
  /** Static confirmation message returned by the API. */
  message: string;
  /** Server UTC time at which the query was handled (ISO 8601). */
  timestampUtc: string;
}

/** Response of `POST /api/demo/echo`. */
export interface IEchoResponse {
  /** The text echoed back by the API. */
  text: string;
  /** Length of the echoed text, in characters. */
  length: number;
}

/**
 * Cross-cutting types used by every package and application.
 * Feature-specific contracts do NOT belong here — they live with the feature
 * or come from the generated SDK.
 */

/** Unique identifier of a persisted entity (GUID string, as the API emits it). */
export type EntityId = string;

/**
 * Page of results returned by list endpoints.
 *
 * @typeParam TItem Type of the items in the page.
 */
export interface IPagedResult<TItem> {
  /** Items of the current page. */
  items: readonly TItem[];
  /** 1-based index of the current page. */
  page: number;
  /** Maximum number of items per page. */
  pageSize: number;
  /** Total number of items across all pages. */
  totalCount: number;
}

/** Query parameters accepted by paginated endpoints. */
export interface IPageRequest {
  /** 1-based page index. */
  page?: number;
  /** Maximum number of items per page. */
  pageSize?: number;
  /** Field to sort by. */
  sortBy?: string;
  /** Sort direction. */
  sortDirection?: SortDirection;
}

/** Direction of a sort. */
export type SortDirection = "asc" | "desc";

/**
 * Makes every property of `T` mutable and required — useful for form models
 * built from readonly API contracts.
 *
 * @typeParam T Source type.
 */
export type Writable<T> = { -readonly [K in keyof T]-?: T[K] };

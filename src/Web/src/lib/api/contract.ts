/**
 * Contract shared by the API and the front end.
 *
 * These types mirror the ASP.NET Core API's OpenAPI document one-for-one and
 * are the only shape either side is allowed to speak. Changing a field here is
 * a deliberate contract change, which is what keeps the client and the server
 * from drifting apart silently.
 */

export type AccountStatus = "active" | "review" | "blocked";
export type AccountSegment = "Corporate" | "Institutional" | "Treasury";

export interface Account {
  readonly id: string;
  readonly name: string;
  readonly iban: string;
  readonly currency: "EUR" | "RON" | "CZK";
  readonly segment: AccountSegment;
  readonly status: AccountStatus;
  /** Minor units, to avoid floating point drift across the wire. */
  readonly balanceMinor: number;
  readonly updatedAt: string;
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly total: number;
  readonly totalPages: number;
  /** Server-reported latency, surfaced in the demo so the wire cost is visible. */
  readonly elapsedMs: number;
}

/** Deliberate scenarios the demo can request, so every state is reachable. */
export type ApiScenario = "ok" | "slow" | "empty" | "error";

export type AccountSortField = "name" | "segment" | "status" | "balanceMinor" | "updatedAt";
export type AccountSortDirection = "asc" | "desc";

export interface AccountsQuery {
  readonly page?: number;
  readonly pageSize?: number;
  readonly search?: string;
  readonly segment?: AccountSegment | "all";
  readonly scenario?: ApiScenario;
  readonly sortBy?: AccountSortField;
  readonly sortDir?: AccountSortDirection;
}

export function formatBalance(minor: number, currency: Account["currency"]) {
  return new Intl.NumberFormat("en-GB", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(minor / 100);
}

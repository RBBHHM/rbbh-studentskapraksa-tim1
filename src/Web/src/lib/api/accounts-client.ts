import { apiClient, buildUrl, type QueryValue } from "@/lib/api/http-client";
import type { Account, AccountsQuery, PagedResult } from "@/lib/api/contract";

/**
 * Typed client for the accounts endpoint.
 *
 * It owns exactly two things: the path and the query shape. Transport, base URL,
 * timeout and error mapping all live in the centralised client in
 * `http-client.ts`, so this file stays a thin, readable description of the
 * contract.
 */

export { ApiError } from "@/lib/api/http-client";

export const ACCOUNTS_ENDPOINT = "/api/public/accounts";

/** Maps the domain query onto wire parameters; defaults are omitted. */
function toParams(query: AccountsQuery): Record<string, QueryValue> {
  return {
    page: query.page,
    pageSize: query.pageSize,
    search: query.search,
    segment: query.segment === "all" ? undefined : query.segment,
    scenario: query.scenario === "ok" ? undefined : query.scenario,
    sortBy: query.sortBy,
    sortDir: query.sortBy ? (query.sortDir ?? "asc") : undefined,
  };
}

/** The exact URL a request will hit — shown in the API demo. */
export function accountsUrl(query: AccountsQuery) {
  return buildUrl(ACCOUNTS_ENDPOINT, toParams(query));
}

export function fetchAccounts(query: AccountsQuery, signal?: AbortSignal): Promise<PagedResult<Account>> {
  return apiClient.get<PagedResult<Account>>(ACCOUNTS_ENDPOINT, { query: toParams(query), signal });
}

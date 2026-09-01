import { useCallback, useMemo, useReducer, useState } from "react";

import { accountSeed } from "@/lib/api/account-data";
import type { Account, AccountSegment, AccountStatus } from "@/lib/api/contract";
import { ALL_VALUES, type AllOr } from "@/lib/admin/account-presentation";

/**
 * The admin panel's data layer.
 *
 * Records live in a reducer rather than in component state so every mutation is
 * an explicit, testable action, and so the table, the metric band and the form
 * all read from one source of truth. Swapping this hook's internals for server
 * functions later requires no change in the screens that consume it.
 */

/** The editable subset of an account — identifiers and audit fields are owned by the store. */
export type AccountDraft = Pick<
  Account,
  "name" | "iban" | "currency" | "segment" | "status" | "balanceMinor"
>;

export const emptyAccountDraft: AccountDraft = {
  name: "",
  iban: "",
  currency: "EUR",
  segment: "Corporate",
  status: "active",
  balanceMinor: 0,
};

export type SortKey = "name" | "segment" | "status" | "balanceMinor" | "updatedAt";
export type SortDirection = "asc" | "desc";

export interface DirectoryFilters {
  readonly search: string;
  readonly segment: AllOr<AccountSegment>;
  readonly status: AllOr<AccountStatus>;
}

export const defaultFilters: DirectoryFilters = {
  search: "",
  segment: ALL_VALUES,
  status: ALL_VALUES,
};

type Action =
  | { type: "create"; draft: AccountDraft; id: string; at: string }
  | { type: "update"; id: string; draft: AccountDraft; at: string }
  | { type: "delete"; id: string }
  | { type: "setStatus"; id: string; status: AccountStatus; at: string }
  | { type: "reset" };

function reducer(state: readonly Account[], action: Action): readonly Account[] {
  switch (action.type) {
    case "create":
      return [{ id: action.id, updatedAt: action.at, ...action.draft }, ...state];
    case "update":
      return state.map((item) =>
        item.id === action.id ? { ...item, ...action.draft, updatedAt: action.at } : item,
      );
    case "setStatus":
      return state.map((item) =>
        item.id === action.id ? { ...item, status: action.status, updatedAt: action.at } : item,
      );
    case "delete":
      return state.filter((item) => item.id !== action.id);
    case "reset":
      return accountSeed;
  }
}

/** Human-readable identifier in the same shape as the seeded records. */
function nextId(records: readonly Account[]) {
  const highest = records.reduce((max, item) => {
    const numeric = Number.parseInt(item.id.replace(/\D/g, ""), 10);
    return Number.isNaN(numeric) ? max : Math.max(max, numeric);
  }, 1000);
  return `AC-${highest + 1}`;
}

export type DraftErrors = Partial<Record<keyof AccountDraft, string>>;

/** Field-level validation, shared by create and edit. Returns "admin.validation.*" ids, resolved by the component. */
export function validateDraft(draft: AccountDraft): DraftErrors {
  const errors: DraftErrors = {};
  if (draft.name.trim().length < 3) errors.name = "name";
  if (draft.iban.replace(/\s/g, "").length < 15) errors.iban = "iban";
  if (!Number.isFinite(draft.balanceMinor) || draft.balanceMinor < 0)
    errors.balanceMinor = "balanceMinor";
  return errors;
}

export function useAccountDirectory() {
  const [records, dispatch] = useReducer(reducer, accountSeed);
  const [filters, setFilters] = useState<DirectoryFilters>(defaultFilters);
  const [sort, setSort] = useState<{ key: SortKey; direction: SortDirection }>({
    key: "updatedAt",
    direction: "desc",
  });

  const visible = useMemo(() => {
    const needle = filters.search.trim().toLowerCase();
    const filtered = records.filter((item) => {
      const matchesNeedle =
        needle.length === 0 ||
        [item.name, item.id, item.iban].some((field) => field.toLowerCase().includes(needle));
      const matchesSegment = filters.segment === ALL_VALUES || item.segment === filters.segment;
      const matchesStatus = filters.status === ALL_VALUES || item.status === filters.status;
      return matchesNeedle && matchesSegment && matchesStatus;
    });

    const factor = sort.direction === "asc" ? 1 : -1;
    return [...filtered].sort((a, b) => {
      const left = a[sort.key];
      const right = b[sort.key];
      if (typeof left === "number" && typeof right === "number") return (left - right) * factor;
      return String(left).localeCompare(String(right)) * factor;
    });
  }, [records, filters, sort]);

  const metrics = useMemo(() => {
    const byStatus = (status: AccountStatus) => records.filter((item) => item.status === status).length;
    /* Reporting currency is EUR; the seed keeps minor units so this is exact. */
    const exposureMinor = records.reduce((total, item) => total + item.balanceMinor, 0);
    return {
      total: records.length,
      exposureMinor,
      averageMinor: records.length === 0 ? 0 : Math.round(exposureMinor / records.length),
      inReview: byStatus("review"),
      blocked: byStatus("blocked"),
      active: byStatus("active"),
    };
  }, [records]);

  const toggleSort = useCallback((key: SortKey) => {
    setSort((current) =>
      current.key === key
        ? { key, direction: current.direction === "asc" ? "desc" : "asc" }
        : { key, direction: key === "name" || key === "segment" ? "asc" : "desc" },
    );
  }, []);

  const patchFilters = useCallback((patch: Partial<DirectoryFilters>) => {
    setFilters((current) => ({ ...current, ...patch }));
  }, []);

  const create = useCallback(
    (draft: AccountDraft) => {
      const id = nextId(records);
      dispatch({ type: "create", draft, id, at: new Date().toISOString() });
      return id;
    },
    [records],
  );

  const update = useCallback((id: string, draft: AccountDraft) => {
    dispatch({ type: "update", id, draft, at: new Date().toISOString() });
  }, []);

  const remove = useCallback((id: string) => dispatch({ type: "delete", id }), []);

  const setStatus = useCallback((id: string, status: AccountStatus) => {
    dispatch({ type: "setStatus", id, status, at: new Date().toISOString() });
  }, []);

  const reset = useCallback(() => {
    dispatch({ type: "reset" });
    setFilters(defaultFilters);
  }, []);

  const isFiltered =
    filters.search !== defaultFilters.search ||
    filters.segment !== defaultFilters.segment ||
    filters.status !== defaultFilters.status;

  return {
    records,
    visible,
    metrics,
    filters,
    isFiltered,
    sort,
    patchFilters,
    clearFilters: () => setFilters(defaultFilters),
    toggleSort,
    create,
    update,
    remove,
    setStatus,
    reset,
  };
}

/** Maps a stored record back to the editable draft the form consumes. */
export function toDraft(account: Account): AccountDraft {
  const { name, iban, currency, segment, status, balanceMinor } = account;
  return { name, iban, currency, segment, status, balanceMinor };
}

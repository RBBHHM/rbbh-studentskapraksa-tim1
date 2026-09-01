import type { Account, AccountSegment, AccountStatus } from "@/lib/api/contract";

/**
 * Presentation metadata for the account domain.
 *
 * Every screen that renders an account — the API demo table, the admin data
 * table, the admin form — reads its tones, option lists and formatters from
 * here. Labels themselves are resolved through the "admin" localization
 * namespace (`status.<id>.label` / `status.<id>.description`) by the
 * components that render them; this module only carries the stable ids.
 */

export type Tone = "brand" | "neutral" | "success" | "warning" | "danger";

export interface StatusMeta {
  readonly tone: Tone;
}

export const accountStatusMeta: Record<AccountStatus, StatusMeta> = {
  active: { tone: "success" },
  review: { tone: "warning" },
  blocked: { tone: "danger" },
};

export const accountStatuses = Object.keys(accountStatusMeta) as readonly AccountStatus[];

export const accountSegmentOptions: readonly AccountSegment[] = [
  "Corporate",
  "Institutional",
  "Treasury",
];

export const accountCurrencies: readonly Account["currency"][] = ["EUR", "RON", "CZK"];

/** Sentinel used by every "all values" filter, so no screen invents its own. */
export const ALL_VALUES = "all" as const;
export type AllOr<T extends string> = T | typeof ALL_VALUES;

export function formatMinor(minor: number, currency: Account["currency"]) {
  return new Intl.NumberFormat("en-GB", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(minor / 100);
}

/**
 * Compact money, formatted deterministically.
 *
 * `Intl.NumberFormat` compact notation differs between ICU builds (the server
 * renders "€746m" where a browser renders "€746.0M"), which would tear the
 * server HTML apart at hydration. The scale suffix is therefore chosen here and
 * only the number itself goes through `Intl`.
 */
const compactScales = [
  { threshold: 1_000_000_000, suffix: "bn" },
  { threshold: 1_000_000, suffix: "m" },
  { threshold: 1_000, suffix: "k" },
] as const;

export function formatCompactMinor(minor: number, currency: Account["currency"] = "EUR") {
  const major = minor / 100;
  const scale = compactScales.find((entry) => Math.abs(major) >= entry.threshold);
  const value = scale ? major / scale.threshold : major;

  return (
    new Intl.NumberFormat("en-GB", {
      style: "currency",
      currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: scale ? 1 : 0,
    }).format(value) + (scale?.suffix ?? "")
  );
}

/** Timestamps are pinned to UTC so the server and the browser print the same string. */
export function formatDateTime(iso: string) {
  return new Intl.DateTimeFormat("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }).format(new Date(iso));
}

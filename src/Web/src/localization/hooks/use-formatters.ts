import * as React from "react";

import { useLocalization } from "../providers/localization-provider";

/**
 * Centralized Intl formatting.
 *
 * Components never build a date pattern or paste a currency symbol; they call
 * these functions so every screen formats consistently for the active locale.
 * Formatter instances are memoized per locale because construction is the
 * expensive part of the Intl APIs.
 */

export interface Formatters {
  readonly locale: string;
  formatDate(value: Date | string | number, options?: Intl.DateTimeFormatOptions): string;
  formatDateTime(value: Date | string | number, options?: Intl.DateTimeFormatOptions): string;
  formatRelativeTime(value: Date | string | number, now?: Date): string;
  formatNumber(value: number, options?: Intl.NumberFormatOptions): string;
  /** Currency is a business value and is always passed separately from the amount. */
  formatCurrency(value: number, currency: string, options?: Intl.NumberFormatOptions): string;
  /** `value` is a ratio: 0.075 → "7.5%". */
  formatPercentage(value: number, options?: Intl.NumberFormatOptions): string;
  formatList(values: readonly string[], type?: Intl.ListFormatOptions["type"]): string;
  formatLocaleName(locale: string): string;
  formatRegionName(region: string): string;
  formatFileSize(bytes: number): string;
}

const INVALID = "—";

function toDate(value: Date | string | number): Date | undefined {
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date;
}

const RELATIVE_UNITS: readonly [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 1000 * 60 * 60 * 24 * 365],
  ["month", 1000 * 60 * 60 * 24 * 30],
  ["week", 1000 * 60 * 60 * 24 * 7],
  ["day", 1000 * 60 * 60 * 24],
  ["hour", 1000 * 60 * 60],
  ["minute", 1000 * 60],
  ["second", 1000],
];

const FILE_SIZE_UNITS = ["byte", "kilobyte", "megabyte", "gigabyte", "terabyte"] as const;

export function createFormatters(locale: string): Formatters {
  const date = new Intl.DateTimeFormat(locale, { dateStyle: "medium" });
  const dateTime = new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" });
  const relative = new Intl.RelativeTimeFormat(locale, { numeric: "auto" });
  const number = new Intl.NumberFormat(locale);
  const percent = new Intl.NumberFormat(locale, { style: "percent", maximumFractionDigits: 1 });
  const list = new Intl.ListFormat(locale, { style: "long", type: "conjunction" });
  const currencyCache = new Map<string, Intl.NumberFormat>();

  const displayNames = (type: Intl.DisplayNamesType) => {
    try {
      return new Intl.DisplayNames([locale], { type });
    } catch {
      return undefined;
    }
  };
  const languageNames = displayNames("language");
  const regionNames = displayNames("region");

  return {
    locale,
    formatDate(value, options) {
      const parsed = toDate(value);
      if (!parsed) return INVALID;
      return options ? new Intl.DateTimeFormat(locale, options).format(parsed) : date.format(parsed);
    },
    formatDateTime(value, options) {
      const parsed = toDate(value);
      if (!parsed) return INVALID;
      return options ? new Intl.DateTimeFormat(locale, options).format(parsed) : dateTime.format(parsed);
    },
    formatRelativeTime(value, now = new Date()) {
      const parsed = toDate(value);
      if (!parsed) return INVALID;
      const deltaMs = parsed.getTime() - now.getTime();
      const match = RELATIVE_UNITS.find(([, size]) => Math.abs(deltaMs) >= size) ?? RELATIVE_UNITS.at(-1)!;
      return relative.format(Math.round(deltaMs / match[1]), match[0]);
    },
    formatNumber(value, options) {
      if (!Number.isFinite(value)) return INVALID;
      return options ? new Intl.NumberFormat(locale, options).format(value) : number.format(value);
    },
    formatCurrency(value, currency, options) {
      if (!Number.isFinite(value)) return INVALID;
      const cacheKey = `${currency}:${JSON.stringify(options ?? {})}`;
      let formatter = currencyCache.get(cacheKey);
      if (!formatter) {
        formatter = new Intl.NumberFormat(locale, { style: "currency", currency, ...options });
        currencyCache.set(cacheKey, formatter);
      }
      return formatter.format(value);
    },
    formatPercentage(value, options) {
      if (!Number.isFinite(value)) return INVALID;
      return options ? new Intl.NumberFormat(locale, { style: "percent", ...options }).format(value) : percent.format(value);
    },
    formatList(values, type) {
      const items = values.filter((item) => item.length > 0);
      if (items.length === 0) return INVALID;
      return type ? new Intl.ListFormat(locale, { style: "long", type }).format(items) : list.format(items);
    },
    formatLocaleName(code) {
      return languageNames?.of(code) ?? code;
    },
    formatRegionName(region) {
      return regionNames?.of(region) ?? region;
    },
    formatFileSize(bytes) {
      if (!Number.isFinite(bytes) || bytes < 0) return INVALID;
      let size = bytes;
      let unitIndex = 0;
      while (size >= 1024 && unitIndex < FILE_SIZE_UNITS.length - 1) {
        size /= 1024;
        unitIndex += 1;
      }
      return new Intl.NumberFormat(locale, {
        style: "unit",
        unit: FILE_SIZE_UNITS[unitIndex],
        unitDisplay: "short",
        maximumFractionDigits: size < 10 && unitIndex > 0 ? 1 : 0,
      }).format(size);
    },
  };
}

const cache = new Map<string, Formatters>();

/** Cached across renders and components: one formatter set per locale. */
export function getFormatters(locale: string): Formatters {
  let formatters = cache.get(locale);
  if (!formatters) {
    formatters = createFormatters(locale);
    cache.set(locale, formatters);
  }
  return formatters;
}

export function useFormatters(): Formatters {
  const { locale } = useLocalization();
  return React.useMemo(() => getFormatters(locale), [locale]);
}

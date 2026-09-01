import * as React from "react";
import { useTranslation } from "react-i18next";

import type { ResourceNamespace } from "../types/resource-keys";

/**
 * Safe translation access.
 *
 * One rule: a key that does not resolve renders nothing. Never a raw key, never
 * an `[object Object]`, never a crash from calling `.map` on a string. Components
 * read copy through this hook so the behaviour is identical on every surface.
 *
 * - `text(key)` returns the resolved string, or "" when the key is missing.
 * - `list(key)` always returns a string array, whatever the bundle holds.
 * - `has(key)` lets a component skip a whole block when copy is absent.
 */

/** Normalizes any translation value into a string array. */
export function toStringList(value: unknown): readonly string[] {
  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === "string" && item.length > 0);
  }
  if (value && typeof value === "object") {
    return Object.values(value).filter(
      (item): item is string => typeof item === "string" && item.length > 0,
    );
  }
  return typeof value === "string" && value.length > 0 ? [value] : [];
}

/** Normalizes any translation value into a string, or "" when unusable. */
export function toText(value: unknown): string {
  return typeof value === "string" ? value : "";
}

export interface SafeTranslation {
  readonly text: (key: string, options?: Record<string, unknown>) => string;
  readonly list: (key: string) => readonly string[];
  readonly has: (key: string) => boolean;
  readonly locale: string;
}

export function useSafeTranslation(namespace: ResourceNamespace): SafeTranslation {
  const { t, i18n } = useTranslation(namespace);

  return React.useMemo<SafeTranslation>(
    () => ({
      text: (key, options) => toText(t(key as never, options as never)),
      list: (key) => toStringList(t(key as never, { returnObjects: true } as never)),
      has: (key) => toText(t(key as never)).length > 0,
      locale: i18n.language,
    }),
    [t, i18n.language],
  );
}

import {
  isDevelopmentOnlyLocale,
  developmentOnlyLocalesEnabled,
  localePreferenceCookieMaxAgeSeconds,
  localePreferenceCookieName,
} from "./localization-config";
import {
  enabledLocales,
  type LocalizationManifest,
  type SupportedLocale,
} from "../types/localization-manifest";

/**
 * Centralized locale resolution.
 *
 * Priority (highest first):
 *   1. authenticated user profile preference
 *   2. locale explicitly present in the URL (e.g. /de/components)
 *   3. RBI-owned preference cookie
 *   4. browser language preferences
 *   5. manifest default locale
 *
 * Nothing here guesses: an unsupported request always falls back through the
 * documented chain instead of silently picking an arbitrary language.
 */

export type LocaleSource = "userProfile" | "url" | "cookie" | "browser" | "manifestDefault";

export interface LocaleResolutionInput {
  readonly manifest: LocalizationManifest;
  readonly userProfileLocale?: string | null | undefined;
  readonly pathname?: string | undefined;
  readonly cookieHeader?: string | null | undefined;
  readonly browserLocales?: readonly string[] | undefined;
}

export interface LocaleResolution {
  readonly locale: string;
  readonly source: LocaleSource;
  readonly entry: SupportedLocale;
  /** Fallback chain used by i18next, most specific first. */
  readonly fallbackChain: readonly string[];
}

function candidates(manifest: LocalizationManifest): readonly SupportedLocale[] {
  const base = enabledLocales(manifest);
  if (!developmentOnlyLocalesEnabled()) return base;
  return base;
}

/**
 * Normalize a requested tag against the manifest.
 *
 * `bs-Latn-BA` → `bs`, `de-AT` → `de`; an unknown language returns undefined so
 * the caller can continue down the priority list.
 */
export function normalizeLocale(
  requested: string | null | undefined,
  manifest: LocalizationManifest,
): string | undefined {
  if (!requested) return undefined;
  const trimmed = requested.trim();
  if (!trimmed) return undefined;

  const supported = candidates(manifest);
  const available = new Map(supported.map((entry) => [entry.code.toLowerCase(), entry.code]));

  // Development-only locales bypass the manifest but only in a dev build.
  if (isDevelopmentOnlyLocale(trimmed) && developmentOnlyLocalesEnabled()) return trimmed;

  const exact = available.get(trimmed.toLowerCase());
  if (exact) return exact;

  const segments = trimmed.split("-");
  // Progressively drop subtags: bs-Latn-BA → bs-Latn → bs.
  for (let length = segments.length - 1; length >= 1; length -= 1) {
    const candidate = segments.slice(0, length).join("-").toLowerCase();
    const match = available.get(candidate);
    if (match) return match;
  }

  // Finally match on primary language subtag against any supported region variant.
  const primary = segments[0]?.toLowerCase();
  if (!primary) return undefined;
  const byLanguage = supported.find((entry) => entry.code.toLowerCase().split("-")[0] === primary);
  return byLanguage?.code;
}

/** Read `/de/components` style prefixes. Returns undefined for unprefixed paths. */
export function localeFromPathname(
  pathname: string | undefined,
  manifest: LocalizationManifest,
): string | undefined {
  if (!pathname) return undefined;
  const first = pathname.split("/").filter(Boolean)[0];
  if (!first) return undefined;
  return normalizeLocale(first, manifest);
}

export function readLocaleCookie(cookieHeader: string | null | undefined): string | undefined {
  if (!cookieHeader) return undefined;
  for (const part of cookieHeader.split(";")) {
    const [name, ...rest] = part.trim().split("=");
    if (name === localePreferenceCookieName) return decodeURIComponent(rest.join("="));
  }
  return undefined;
}

export function resolveLocale(input: LocaleResolutionInput): LocaleResolution {
  const { manifest } = input;

  const ordered: readonly [LocaleSource, string | undefined][] = [
    ["userProfile", normalizeLocale(input.userProfileLocale, manifest)],
    ["url", localeFromPathname(input.pathname, manifest)],
    ["cookie", normalizeLocale(readLocaleCookie(input.cookieHeader), manifest)],
    ...(input.browserLocales ?? []).map(
      (tag) => ["browser", normalizeLocale(tag, manifest)] as [LocaleSource, string | undefined],
    ),
  ];

  const hit = ordered.find(([, locale]) => Boolean(locale));
  const locale = hit?.[1] ?? manifest.defaultLocale;
  const source: LocaleSource = hit ? hit[0] : "manifestDefault";

  return {
    locale,
    source,
    entry: entryFor(manifest, locale),
    fallbackChain: buildFallbackChain(locale, manifest),
  };
}

function entryFor(manifest: LocalizationManifest, locale: string): SupportedLocale {
  const found = manifest.supportedLocales.find((entry) => entry.code === locale);
  if (found) return found;
  // Development-only locales are not in the manifest; describe them explicitly.
  if (isDevelopmentOnlyLocale(locale)) {
    return {
      code: locale,
      displayNameKey: `locales.${locale}`,
      direction: locale === "ar-XB" ? "rtl" : "ltr",
      enabled: true,
    };
  }
  const fallback = manifest.supportedLocales.find((entry) => entry.code === manifest.defaultLocale);
  if (!fallback) throw new Error("Manifest has no default locale entry");
  return fallback;
}

/** de-AT → [de-AT, de, <fallbackLocale>, <defaultLocale>], de-duplicated. */
export function buildFallbackChain(
  locale: string,
  manifest: LocalizationManifest,
): readonly string[] {
  const chain: string[] = [locale];
  const segments = locale.split("-");
  for (let length = segments.length - 1; length >= 1; length -= 1) {
    chain.push(segments.slice(0, length).join("-"));
  }
  chain.push(manifest.fallbackLocale, manifest.defaultLocale);
  const supported = new Set([
    ...manifest.supportedLocales.map((entry) => entry.code),
    locale,
  ]);
  return Array.from(new Set(chain)).filter((code) => supported.has(code) || isDevelopmentOnlyLocale(code));
}

/** Persist the choice in an RBI-owned, non-sensitive preference cookie. */
export function persistLocalePreference(locale: string): void {
  if (typeof document === "undefined") return;
  const secure = window.location.protocol === "https:" ? "; Secure" : "";
  document.cookie = `${localePreferenceCookieName}=${encodeURIComponent(locale)}; Path=/; Max-Age=${localePreferenceCookieMaxAgeSeconds}; SameSite=Lax${secure}`;
}

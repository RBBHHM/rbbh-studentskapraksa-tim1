import { localizationEnvironment } from "./localization-environment";

/**
 * Static localization policy: namespace registry and cookie/telemetry names.
 *
 * The supported *locale list* deliberately does NOT live here — it comes from
 * the runtime manifest so a new language ships without a frontend deployment.
 */

/** Every namespace the application may request. Feature-oriented, not per-component. */
export const localizationNamespaces = [
  "common",
  "navigation",
  "forms",
  "validation",
  "errors",
  "ui-library",
  "api-demo",
  "accessibility",
  "date-time",
  "overview",
  "foundations",
  "components",
  "patterns",
  "architecture",
  "admin",
  "registry",
] as const;

export type AppNamespace = (typeof localizationNamespaces)[number];

/** Small, shared namespaces loaded before first paint. Everything else is lazy. */
export const criticalNamespaces = [
  "common",
  "navigation",
] as const satisfies readonly AppNamespace[];

export const defaultNamespace = "common" satisfies AppNamespace;

/** Route → namespaces, so a route loads only what it renders. */
export const routeNamespaces: Readonly<Record<string, readonly AppNamespace[]>> = {
  "/": ["common", "navigation", "overview"],
  "/foundations": ["ui-library", "foundations"],
  "/components": ["ui-library", "components", "forms", "validation", "accessibility"],
  "/patterns": ["ui-library", "patterns", "forms", "validation"],
  "/applications": ["api-demo", "admin", "forms", "validation", "errors"],
  "/architecture": ["ui-library", "architecture"],
  "/app": ["registry", "errors"],
};

export function namespacesForRoute(pathname: string): readonly AppNamespace[] {
  const match = Object.keys(routeNamespaces)
    .filter((route) => (route === "/" ? pathname === "/" : pathname.startsWith(route)))
    .sort((a, b) => b.length - a.length)[0];
  const routed = match ? (routeNamespaces[match] ?? []) : [];
  return Array.from(new Set([...criticalNamespaces, ...routed]));
}

/** RBI-owned preference cookie. No third party reads or writes it. */
export const localePreferenceCookieName = "rbi.locale";
export const localePreferenceCookieMaxAgeSeconds = 60 * 60 * 24 * 365;

/** Development-only locales. Never listed in a production manifest. */
export const pseudoLocaleCode = "en-XA";
export const rtlDevelopmentLocaleCode = "ar-XB";

export function isDevelopmentOnlyLocale(code: string): boolean {
  return code === pseudoLocaleCode || code === rtlDevelopmentLocaleCode;
}

export function developmentOnlyLocalesEnabled(): boolean {
  return localizationEnvironment.enablePseudoLocale;
}

/** Bounded retries: a failing CDN must not turn into a request storm. */
export const maxBundleLoadRetries = 2;
export const maxManifestLoadRetries = 1;

/** Defensive response ceiling for localization payloads (bytes). */
export const maxLocalizationResponseBytes = 1_024 * 1_024;

import * as React from "react";
import { I18nextProvider } from "react-i18next";
import type { i18n as I18nInstance } from "i18next";

import { createLocalizationClient } from "../client/localization-client";
import { loadManifest } from "../client/manifest-client";
import {
  namespacesForRoute,
  criticalNamespaces,
} from "../config/localization-config";
import { localizationEnvironment } from "../config/localization-environment";
import {
  buildFallbackChain,
  persistLocalePreference,
  resolveLocale,
  normalizeLocale,
} from "../config/locale-resolution";
import { emitLocalizationTelemetry } from "../telemetry/localization-telemetry";
import {
  enabledLocales,
  type LocalizationManifest,
  type SupportedLocale,
  type TextDirection,
} from "../types/localization-manifest";

/**
 * Application-level localization provider.
 *
 * Owns the whole runtime lifecycle: manifest load and revalidation, locale
 * resolution, single i18next instance, `<html lang>`/`<html dir>`, lazy route
 * namespaces, release changes, fallbacks, error state and telemetry.
 *
 * SSR-safe: the first render is always the loading state, so server and client
 * markup match; every network read happens in an effect after hydration.
 */

export type LocalizationStatus = "loading" | "ready" | "error";

export interface LocalizationContextValue {
  readonly status: LocalizationStatus;
  readonly manifest: LocalizationManifest | undefined;
  readonly releaseId: string | undefined;
  readonly locale: string;
  readonly direction: TextDirection;
  readonly availableLocales: readonly SupportedLocale[];
  readonly error: Error | undefined;
  readonly setLocale: (locale: string) => void;
  readonly retry: () => void;
  readonly loadNamespaces: (namespaces: readonly string[]) => Promise<void>;
}

const LocalizationContext = React.createContext<LocalizationContextValue | null>(null);

export function useLocalization(): LocalizationContextValue {
  const context = React.useContext(LocalizationContext);
  if (!context) {
    throw new Error("useLocalization must be used inside <LocalizationProvider>");
  }
  return context;
}

export interface LocalizationProviderProps {
  readonly children: React.ReactNode;
  /** Locale from an authenticated user profile; highest resolution priority. */
  readonly userProfileLocale?: string | null | undefined;
  /** Current pathname, so `/de/components` style URLs win over the cookie. */
  readonly pathname?: string | undefined;
  /** Rendered while the manifest and critical namespaces load. */
  readonly loadingFallback?: React.ReactNode;
  /** Rendered when localization cannot be initialised at all. */
  readonly errorFallback?: (state: { error: Error; retry: () => void }) => React.ReactNode;
}

interface ProviderState {
  readonly status: LocalizationStatus;
  readonly manifest?: LocalizationManifest;
  readonly instance?: I18nInstance;
  readonly locale: string;
  readonly direction: TextDirection;
  readonly error?: Error;
}

export function LocalizationProvider({
  children,
  userProfileLocale,
  pathname,
  loadingFallback = null,
  errorFallback,
}: LocalizationProviderProps) {
  const [attempt, setAttempt] = React.useState(0);
  const [state, setState] = React.useState<ProviderState>({
    status: "loading",
    locale: "en",
    direction: "ltr",
  });

  const applyDocumentAttributes = React.useCallback((locale: string, direction: TextDirection) => {
    if (typeof document === "undefined") return;
    document.documentElement.lang = locale;
    document.documentElement.dir = direction;
  }, []);

  // Bootstrap: manifest → locale → i18next. Runs once per retry attempt.
  React.useEffect(() => {
    let cancelled = false;
    const controller = new AbortController();

    (async () => {
      try {
        const { manifest } = await loadManifest(controller.signal);
        const resolution = resolveLocale({
          manifest,
          userProfileLocale,
          pathname: pathname ?? (typeof window === "undefined" ? undefined : window.location.pathname),
          cookieHeader: typeof document === "undefined" ? undefined : document.cookie,
          browserLocales: typeof navigator === "undefined" ? [] : [...navigator.languages],
        });

        const instance = await createLocalizationClient({
          manifest,
          locale: resolution.locale,
          fallbackChain: resolution.fallbackChain,
          namespaces: [...criticalNamespaces],
        });

        if (cancelled) return;
        emitLocalizationTelemetry("localization_locale_changed", {
          releaseId: manifest.releaseId,
          resolvedLocale: resolution.locale,
          requestedLocale: resolution.locale,
        });
        applyDocumentAttributes(resolution.locale, resolution.entry.direction);
        setState({
          status: "ready",
          manifest,
          instance,
          locale: resolution.locale,
          direction: resolution.entry.direction,
        });
      } catch (error) {
        if (cancelled) return;
        setState((previous) => ({
          ...previous,
          status: "error",
          error: error instanceof Error ? error : new Error("Localization unavailable"),
        }));
      }
    })();

    return () => {
      cancelled = true;
      controller.abort();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- retry is driven by `attempt`
  }, [attempt, userProfileLocale, applyDocumentAttributes]);

  // Manifest revalidation: pick up a newly published release without a deploy.
  React.useEffect(() => {
    const instance = state.instance;
    const manifest = state.manifest;
    if (state.status !== "ready" || !instance || !manifest) return;

    const timer = window.setInterval(() => {
      void (async () => {
        try {
          const { manifest: latest } = await loadManifest();
          if (latest.releaseId === manifest.releaseId) return;
          const loaded = Object.keys(
            (instance.services.resourceStore.data[state.locale] ?? {}) as Record<string, unknown>,
          );
          const next = await createLocalizationClient({
            manifest: latest,
            locale: state.locale,
            fallbackChain: buildFallbackChain(state.locale, latest),
            namespaces: loaded.length > 0 ? loaded : [...criticalNamespaces],
          });
          emitLocalizationTelemetry("localization_release_changed", {
            releaseId: latest.releaseId,
            resolvedLocale: state.locale,
          });
          setState((previous) => ({ ...previous, manifest: latest, instance: next }));
        } catch {
          // Revalidation failures are non-fatal: keep serving the active release.
        }
      })();
    }, localizationEnvironment.manifestRevalidationMs);

    return () => window.clearInterval(timer);
  }, [state.status, state.instance, state.manifest, state.locale]);

  const loadNamespaces = React.useCallback(
    async (namespaces: readonly string[]) => {
      const instance = state.instance;
      if (!instance || namespaces.length === 0) return;
      await instance.loadNamespaces([...namespaces]);
    },
    [state.instance],
  );

  // Lazy route namespaces.
  React.useEffect(() => {
    if (state.status !== "ready") return;
    const currentPath = pathname ?? (typeof window === "undefined" ? "/" : window.location.pathname);
    void loadNamespaces(namespacesForRoute(currentPath));
  }, [state.status, pathname, loadNamespaces]);

  const setLocale = React.useCallback(
    (requested: string) => {
      const instance = state.instance;
      const manifest = state.manifest;
      if (!instance || !manifest) return;
      const next = normalizeLocale(requested, manifest) ?? manifest.defaultLocale;
      const entry =
        manifest.supportedLocales.find((candidate) => candidate.code === next) ??
        manifest.supportedLocales.find((candidate) => candidate.code === manifest.defaultLocale);
      const direction = entry?.direction ?? "ltr";

      persistLocalePreference(next);
      void instance.changeLanguage(next).then(() => {
        applyDocumentAttributes(next, direction);
        emitLocalizationTelemetry("localization_locale_changed", {
          releaseId: manifest.releaseId,
          requestedLocale: requested,
          resolvedLocale: next,
        });
        setState((previous) => ({ ...previous, locale: next, direction }));
      });
    },
    [state.instance, state.manifest, applyDocumentAttributes],
  );

  const retry = React.useCallback(() => setAttempt((value) => value + 1), []);

  const value = React.useMemo<LocalizationContextValue>(
    () => ({
      status: state.status,
      manifest: state.manifest,
      releaseId: state.manifest?.releaseId,
      locale: state.locale,
      direction: state.direction,
      availableLocales: state.manifest ? enabledLocales(state.manifest) : [],
      error: state.error,
      setLocale,
      retry,
      loadNamespaces,
    }),
    [state, setLocale, retry, loadNamespaces],
  );

  if (state.status === "error" && errorFallback && state.error) {
    return (
      <LocalizationContext.Provider value={value}>
        {errorFallback({ error: state.error, retry })}
      </LocalizationContext.Provider>
    );
  }

  if (state.status !== "ready" || !state.instance) {
    return (
      <LocalizationContext.Provider value={value}>{loadingFallback}</LocalizationContext.Provider>
    );
  }

  return (
    <LocalizationContext.Provider value={value}>
      <I18nextProvider i18n={state.instance}>{children}</I18nextProvider>
    </LocalizationContext.Provider>
  );
}

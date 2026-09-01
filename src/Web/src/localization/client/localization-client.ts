import i18next, { type i18n as I18nInstance } from "i18next";
import ICU from "i18next-icu";
import HttpBackend, { type HttpBackendOptions } from "i18next-http-backend";

import {
  criticalNamespaces,
  defaultNamespace,
  isDevelopmentOnlyLocale,
  localizationNamespaces,
  maxBundleLoadRetries,
  pseudoLocaleCode,
  rtlDevelopmentLocaleCode,
} from "../config/localization-config";
import { localizationEnvironment } from "../config/localization-environment";
import {
  categorizeError,
  emitLocalizationTelemetry,
} from "../telemetry/localization-telemetry";
import type { LocalizationManifest } from "../types/localization-manifest";
import { pseudoLocalePostProcessor, pseudoLocalePostProcessorName } from "../testing/pseudo-localization";
import { bundleLoadPathTemplate } from "./bundle-path-resolver";
import { fetchLocalizationJson } from "./localization-http";

/**
 * The one and only i18next instance.
 *
 * Features initialise it here and nowhere else — no page component, no feature
 * module may call `init`. Bundles are fetched at runtime from the immutable
 * release referenced by the manifest, so translations are never part of the
 * JavaScript bundle.
 */

export interface CreateLocalizationClientInput {
  readonly manifest: LocalizationManifest;
  readonly locale: string;
  readonly fallbackChain: readonly string[];
  readonly namespaces?: readonly string[];
}

/** Custom loader so every bundle read shares our JSON-only, timed fetch. */
function backendOptions(manifest: LocalizationManifest): HttpBackendOptions {
  return {
    loadPath: bundleLoadPathTemplate(localizationEnvironment.manifestUrl, manifest.releaseId),
    async request(_options, url, _payload, callback) {
      const startedAt = Date.now();
      const namespace = url.split("/").pop()?.replace(/\.json$/, "");
      emitLocalizationTelemetry("localization_bundle_load_started", {
        releaseId: manifest.releaseId,
        ...(namespace ? { namespace } : {}),
      });
      for (let attempt = 0; attempt <= maxBundleLoadRetries; attempt += 1) {
        try {
          const result = await fetchLocalizationJson<Record<string, unknown>>(url, {
            timeoutMs: localizationEnvironment.requestTimeoutMs,
            // Versioned URLs are immutable, so the HTTP cache may serve them freely.
            cache: "default",
          });
          emitLocalizationTelemetry("localization_bundle_load_succeeded", {
            releaseId: manifest.releaseId,
            ...(namespace ? { namespace } : {}),
            durationMs: Date.now() - startedAt,
          });
          callback(null, { status: 200, data: (result.payload ?? {}) as never });
          return;
        } catch (error) {
          if (attempt === maxBundleLoadRetries) {
            emitLocalizationTelemetry("localization_bundle_load_failed", {
              releaseId: manifest.releaseId,
              ...(namespace ? { namespace } : {}),
              errorCategory: categorizeError(error),
            });
            callback(error as Error, { status: 500, data: "" as never });
            return;
          }
        }
      }
    },
  };
}

export function createLocalizationClient(input: CreateLocalizationClientInput): Promise<I18nInstance> {
  const instance = i18next.createInstance();
  const dev = localizationEnvironment.enablePseudoLocale;

  let chain = instance.use(ICU).use(HttpBackend);
  if (dev) chain = chain.use(pseudoLocalePostProcessor);

  return chain
    .init<HttpBackendOptions>({
      lng: input.locale,
      fallbackLng: [...input.fallbackChain],
      supportedLngs: [
        ...input.fallbackChain,
        ...input.manifest.supportedLocales.map((entry) => entry.code),
        ...(dev ? [pseudoLocaleCode, rtlDevelopmentLocaleCode] : []),
      ],
      // Development locales have no published bundles; they reuse the source locale.
      load: "currentOnly",
      ns: input.namespaces ?? [...criticalNamespaces],
      defaultNS: defaultNamespace,
      fallbackNS: false,
      partialBundledLanguages: false,
      interpolation: { escapeValue: false },
      returnNull: false,
      // Keys must be resolved from bundles; missing keys are reported, not invented.
      // A key that does not resolve renders nothing, in every environment; gaps are
      // reported through telemetry instead of leaking into the interface.
      parseMissingKeyHandler: () => "",
      saveMissing: false,
      debug: localizationEnvironment.debug,
      backend: backendOptions(input.manifest),
      ...(dev ? { postProcess: [pseudoLocalePostProcessorName] } : {}),
      react: { useSuspense: false },
    })
    .then(() => instance);
}

/** Namespaces known to the application, used for validation and tooling. */
export const knownNamespaces: readonly string[] = localizationNamespaces;

export function isDevelopmentLocale(code: string): boolean {
  return isDevelopmentOnlyLocale(code);
}

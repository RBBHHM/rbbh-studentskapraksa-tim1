/**
 * Public localization API.
 *
 * Application code imports from `@/localization` only. Internals (http client,
 * manifest client, bundle path resolver) stay private so the delivery mechanism
 * can change without touching feature code.
 */

export { LocalizationProvider, useLocalization } from "./providers/localization-provider";
export type { LocalizationContextValue, LocalizationStatus } from "./providers/localization-provider";

export { useLocale } from "./hooks/use-locale";
export { useFormatters, getFormatters, createFormatters } from "./hooks/use-formatters";
export type { Formatters } from "./hooks/use-formatters";
export { useLocalizedNavigation } from "./hooks/use-localized-navigation";
export { useSafeTranslation, toStringList, toText } from "./hooks/use-safe-translation";
export type { SafeTranslation } from "./hooks/use-safe-translation";

export { LanguageSwitcher } from "./components/language-switcher";
export { LocalizationLoadingState } from "./components/localization-loading-state";
export { LocalizationErrorState } from "./components/localization-error-state";
export { MissingTranslationIndicator } from "./components/missing-translation-indicator";

export {
  localizationNamespaces,
  criticalNamespaces,
  defaultNamespace,
  namespacesForRoute,
  localePreferenceCookieName,
} from "./config/localization-config";
export type { AppNamespace } from "./config/localization-config";

export { localizationEnvironment } from "./config/localization-environment";

export {
  resolveLocale,
  normalizeLocale,
  buildFallbackChain,
  persistLocalePreference,
} from "./config/locale-resolution";
export type { LocaleResolution, LocaleSource } from "./config/locale-resolution";

export {
  registerLocalizationTelemetrySink,
  emitLocalizationTelemetry,
} from "./telemetry/localization-telemetry";
export type {
  LocalizationTelemetryEvent,
  LocalizationTelemetryDimensions,
  LocalizationTelemetrySink,
} from "./telemetry/localization-telemetry";

export {
  backendErrorCodes,
  errorTranslationKey,
  isKnownBackendErrorCode,
  useLocalizedApiError,
} from "./errors/error-code-map";
export type { BackendErrorCode } from "./errors/error-code-map";

export { validateManifest, enabledLocales } from "./types/localization-manifest";
export type {
  LocalizationManifest,
  SupportedLocale,
  TextDirection,
} from "./types/localization-manifest";
export type {
  LocalizationResources,
  ResourceNamespace,
  TranslationKey,
  QualifiedTranslationKey,
} from "./types/resource-keys";

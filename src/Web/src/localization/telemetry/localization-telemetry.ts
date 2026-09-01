import { localizationEnvironment } from "../config/localization-environment";

/**
 * Structured localization telemetry.
 *
 * Emits non-sensitive dimensions only: never translated text, never customer
 * input. Consumers (Application Insights, a collector endpoint) subscribe via
 * `registerLocalizationTelemetrySink`; by default events are debug-logged only.
 */

export type LocalizationTelemetryEvent =
  | "localization_manifest_load_started"
  | "localization_manifest_load_succeeded"
  | "localization_manifest_load_failed"
  | "localization_release_changed"
  | "localization_bundle_load_started"
  | "localization_bundle_load_succeeded"
  | "localization_bundle_load_failed"
  | "localization_fallback_used"
  | "localization_missing_key"
  | "localization_invalid_manifest"
  | "localization_invalid_bundle"
  | "localization_locale_changed";

export interface LocalizationTelemetryDimensions {
  readonly environment?: string;
  readonly releaseId?: string;
  readonly requestedLocale?: string;
  readonly resolvedLocale?: string;
  readonly fallbackLocale?: string;
  readonly namespace?: string;
  readonly key?: string;
  readonly route?: string;
  readonly errorCategory?: "network" | "timeout" | "http" | "schema" | "unknown";
  readonly durationMs?: number;
  readonly cacheStatus?: "hit" | "revalidated" | "miss" | "last-known-good";
  readonly issues?: readonly string[];
}

export type LocalizationTelemetrySink = (
  event: LocalizationTelemetryEvent,
  dimensions: LocalizationTelemetryDimensions,
) => void;

const sinks = new Set<LocalizationTelemetrySink>();

export function registerLocalizationTelemetrySink(sink: LocalizationTelemetrySink): () => void {
  sinks.add(sink);
  return () => sinks.delete(sink);
}

export function emitLocalizationTelemetry(
  event: LocalizationTelemetryEvent,
  dimensions: LocalizationTelemetryDimensions = {},
): void {
  const enriched: LocalizationTelemetryDimensions = {
    environment: localizationEnvironment.isDevelopment ? "development" : "production",
    ...dimensions,
  };
  if (localizationEnvironment.debug) {
    console.info(`[localization] ${event}`, enriched);
  }
  for (const sink of sinks) {
    try {
      sink(event, enriched);
    } catch {
      // A telemetry sink must never break localization.
    }
  }
}

export function categorizeError(error: unknown): NonNullable<LocalizationTelemetryDimensions["errorCategory"]> {
  if (error instanceof DOMException && error.name === "AbortError") return "timeout";
  if (error instanceof LocalizationHttpError) return "http";
  if (error instanceof LocalizationSchemaError) return "schema";
  if (error instanceof TypeError) return "network";
  return "unknown";
}

export class LocalizationHttpError extends Error {
  constructor(
    readonly url: string,
    readonly status: number,
  ) {
    super(`Localization request failed with HTTP ${status}`);
    this.name = "LocalizationHttpError";
  }
}

export class LocalizationSchemaError extends Error {
  constructor(
    message: string,
    readonly issues: readonly string[] = [],
  ) {
    super(message);
    this.name = "LocalizationSchemaError";
  }
}

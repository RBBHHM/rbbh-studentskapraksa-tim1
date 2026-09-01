import { maxManifestLoadRetries } from "../config/localization-config";
import { localizationEnvironment } from "../config/localization-environment";
import {
  categorizeError,
  emitLocalizationTelemetry,
  LocalizationSchemaError,
} from "../telemetry/localization-telemetry";
import {
  validateManifest,
  type LocalizationManifest,
} from "../types/localization-manifest";
import { fetchLocalizationJson } from "./localization-http";

/**
 * Environment manifest client.
 *
 * The manifest is the only mutable artifact, so it is always revalidated
 * (ETag / If-None-Match, `no-cache`) rather than cached as immutable.
 *
 * Last-known-good: a *validated* manifest is stored so the application keeps
 * working when the delivery endpoint is briefly unavailable. A malformed or
 * partially validated manifest is never stored.
 */

const LAST_KNOWN_GOOD_KEY = "rbi.localization.lastKnownGoodManifest";

export interface ManifestLoadResult {
  readonly manifest: LocalizationManifest;
  readonly cacheStatus: "miss" | "revalidated" | "last-known-good";
}

interface CachedManifest {
  readonly etag: string | undefined;
  readonly manifest: LocalizationManifest;
}

let memoryCache: CachedManifest | undefined;
let inFlight: Promise<ManifestLoadResult> | undefined;

function readLastKnownGood(): LocalizationManifest | undefined {
  if (memoryCache) return memoryCache.manifest;
  if (typeof window === "undefined") return undefined;
  try {
    const raw = window.localStorage.getItem(LAST_KNOWN_GOOD_KEY);
    if (!raw) return undefined;
    const result = validateManifest(JSON.parse(raw) as unknown);
    // Only a manifest that still validates may be reused.
    return result.ok ? result.manifest : undefined;
  } catch {
    return undefined;
  }
}

function writeLastKnownGood(manifest: LocalizationManifest): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(LAST_KNOWN_GOOD_KEY, JSON.stringify(manifest));
  } catch {
    // Storage may be unavailable (private mode, quota) — non-fatal.
  }
}

async function loadOnce(signal?: AbortSignal): Promise<ManifestLoadResult> {
  const url = localizationEnvironment.manifestUrl;
  const startedAt = Date.now();
  emitLocalizationTelemetry("localization_manifest_load_started", {});

  const headers = memoryCache?.etag ? { "if-none-match": memoryCache.etag } : undefined;
  const response = await fetchLocalizationJson<unknown>(url, {
    timeoutMs: localizationEnvironment.requestTimeoutMs,
    cache: "no-cache",
    ...(headers ? { headers } : {}),
    signal,
  });

  if (response.notModified && memoryCache) {
    emitLocalizationTelemetry("localization_manifest_load_succeeded", {
      releaseId: memoryCache.manifest.releaseId,
      durationMs: Date.now() - startedAt,
      cacheStatus: "revalidated",
    });
    return { manifest: memoryCache.manifest, cacheStatus: "revalidated" };
  }

  const validation = validateManifest(response.payload);
  if (!validation.ok) {
    emitLocalizationTelemetry("localization_invalid_manifest", { issues: validation.issues });
    throw new LocalizationSchemaError("Localization manifest failed validation", validation.issues);
  }

  const previous = memoryCache?.manifest.releaseId;
  memoryCache = { etag: response.etag, manifest: validation.manifest };
  writeLastKnownGood(validation.manifest);

  emitLocalizationTelemetry("localization_manifest_load_succeeded", {
    releaseId: validation.manifest.releaseId,
    durationMs: Date.now() - startedAt,
    cacheStatus: "miss",
  });
  if (previous && previous !== validation.manifest.releaseId) {
    emitLocalizationTelemetry("localization_release_changed", {
      releaseId: validation.manifest.releaseId,
    });
  }
  return { manifest: validation.manifest, cacheStatus: "miss" };
}

/**
 * Load (or revalidate) the active environment manifest.
 *
 * Retries are bounded; on exhaustion a validated last-known-good manifest is
 * reused when available, otherwise the error propagates to the error state.
 */
export async function loadManifest(signal?: AbortSignal): Promise<ManifestLoadResult> {
  // Reuse in-flight requests so a burst of consumers issues one network call.
  if (inFlight) return inFlight;

  const request: Promise<ManifestLoadResult> = (async (): Promise<ManifestLoadResult> => {
    let lastError: unknown;
    for (let attempt = 0; attempt <= maxManifestLoadRetries; attempt += 1) {
      try {
        return await loadOnce(signal);
      } catch (error) {
        lastError = error;
        emitLocalizationTelemetry("localization_manifest_load_failed", {
          errorCategory: categorizeError(error),
        });
      }
    }
    const lastKnownGood = readLastKnownGood();
    if (lastKnownGood) {
      emitLocalizationTelemetry("localization_fallback_used", {
        releaseId: lastKnownGood.releaseId,
        cacheStatus: "last-known-good",
      });
      return { manifest: lastKnownGood, cacheStatus: "last-known-good" };
    }
    throw lastError instanceof Error ? lastError : new Error("Localization manifest unavailable");
  })().finally(() => {
    inFlight = undefined;
  });

  inFlight = request;
  return request;
}

/** Test seam: clear caches between tests. */
export function resetManifestCache(): void {
  memoryCache = undefined;
  inFlight = undefined;
  if (typeof window !== "undefined") {
    try {
      window.localStorage.removeItem(LAST_KNOWN_GOOD_KEY);
    } catch {
      /* ignore */
    }
  }
}

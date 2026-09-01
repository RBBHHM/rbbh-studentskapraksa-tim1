import { ApiError, apiClient } from "./http-client";

export type LegacyRecord = Readonly<Record<string, unknown>>;

interface LegacyEnvelope {
  readonly data?: unknown;
  readonly items?: unknown;
  readonly results?: unknown;
  readonly value?: unknown;
}

/**
 * Temporary anti-corruption boundary for the existing service. Legacy
 * controllers return several response shapes; that inconsistency is handled
 * once here and never leaks into screens. Remove the normaliser when the
 * backend adopts the template ApiResponse<T> contract.
 */
export async function getLegacyRecords(path: string): Promise<readonly LegacyRecord[]> {
  const payload = await apiClient.getLegacy<unknown>(path);
  const candidate = unwrapCollection(payload);

  if (!Array.isArray(candidate)) {
    throw new ApiError(
      502,
      { code: "legacy_response_shape", message: "The service did not return a collection." },
      "legacy-boundary",
    );
  }

  return candidate.filter(isRecord);
}

function unwrapCollection(payload: unknown): unknown {
  if (Array.isArray(payload)) return payload;
  if (!isRecord(payload)) return payload;

  const envelope = payload as LegacyEnvelope;
  for (const candidate of [envelope.data, envelope.items, envelope.results, envelope.value]) {
    if (Array.isArray(candidate)) return candidate;
    if (isRecord(candidate)) {
      const nested = unwrapCollection(candidate);
      if (Array.isArray(nested)) return nested;
    }
  }

  // Older controllers use domain-specific envelope names (users, roles,
  // categories). Keep that wire-format knowledge in this boundary.
  for (const candidate of Object.values(payload)) {
    if (Array.isArray(candidate)) return candidate;
  }

  return payload;
}

function isRecord(value: unknown): value is LegacyRecord {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

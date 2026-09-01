import { maxLocalizationResponseBytes } from "../config/localization-config";
import { LocalizationHttpError } from "../telemetry/localization-telemetry";

/**
 * The single low-level fetch used for every localization request.
 *
 * Enforces: JSON only (never evaluated as script), an explicit timeout, and a
 * defensive response size ceiling. Localization content is data, never code.
 */

export interface JsonFetchOptions {
  readonly timeoutMs: number;
  readonly headers?: Readonly<Record<string, string>>;
  readonly cache?: RequestCache;
  readonly signal?: AbortSignal | undefined;
}

export interface JsonFetchResult<T> {
  readonly payload: T | undefined;
  readonly status: number;
  readonly etag: string | undefined;
  readonly notModified: boolean;
}

export async function fetchLocalizationJson<T = unknown>(
  url: string,
  options: JsonFetchOptions,
): Promise<JsonFetchResult<T>> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), options.timeoutMs);
  options.signal?.addEventListener("abort", () => controller.abort(), { once: true });

  try {
    const response = await fetch(url, {
      method: "GET",
      headers: { accept: "application/json", ...options.headers },
      ...(options.cache ? { cache: options.cache } : {}),
      signal: controller.signal,
      // `credentials` is intentionally left at the default: some hosting/preview
      // proxies reject cross-context requests made with `omit`, and localization
      // bundles are public, non-authenticated content anyway.
      redirect: "follow",
    });

    const etag = response.headers.get("etag") ?? undefined;
    if (response.status === 304) {
      return { payload: undefined, status: 304, etag, notModified: true };
    }
    if (!response.ok) throw new LocalizationHttpError(url, response.status);

    const declaredLength = Number(response.headers.get("content-length") ?? "0");
    if (declaredLength > maxLocalizationResponseBytes) {
      throw new LocalizationHttpError(url, 413);
    }

    const text = await response.text();
    if (text.length > maxLocalizationResponseBytes) throw new LocalizationHttpError(url, 413);

    return { payload: JSON.parse(text) as T, status: response.status, etag, notModified: false };
  } finally {
    clearTimeout(timeout);
  }
}

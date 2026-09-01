import { getAccessToken } from "../auth/keycloak";
import { runtimeConfig } from "../../runtime-config";

/** One wire format for every successful and failed application API response. */
export interface ApiEnvelope<T> {
  readonly success: boolean;
  readonly data: T | null;
  readonly error: ApiFailure | null;
  readonly traceId: string;
}

export interface ApiFailure {
  readonly code: string;
  readonly message: string;
  readonly details?: Readonly<Record<string, readonly string[]>> | null;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly traceId: string;
  readonly details?: ApiFailure["details"];

  constructor(status: number, failure: ApiFailure, traceId: string) {
    super(failure.message);
    this.name = "ApiError";
    this.status = status;
    this.code = failure.code;
    this.traceId = traceId;
    this.details = failure.details;
  }
}

export const apiBaseUrl: string =
  runtimeConfig.API_BASE_URL?.replace(/\/$/, "") ?? "";

const REQUEST_TIMEOUT_MS = 15_000;
export type QueryValue = string | number | boolean | undefined | null;

export interface ApiRequestOptions {
  readonly query?: Readonly<Record<string, QueryValue>> | undefined;
  readonly signal?: AbortSignal | undefined;
  readonly timeoutMs?: number | undefined;
  readonly headers?: Readonly<Record<string, string>> | undefined;
}

export interface ApiBodyRequestOptions extends ApiRequestOptions {
  readonly body?: unknown | undefined;
}

export function buildUrl(path: string, query?: ApiRequestOptions["query"]): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === undefined || value === null || value === "") continue;
    params.set(key, String(value));
  }
  const search = params.toString();
  return `${apiBaseUrl}${path}${search ? `?${search}` : ""}`;
}

export interface ApiClient {
  get<T>(path: string, options?: ApiRequestOptions): Promise<T>;
  /** Temporary bridge for controllers not yet migrated to ApiEnvelope<T>. */
  getLegacy<T>(path: string, options?: ApiRequestOptions): Promise<T>;
  postLegacy<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  putLegacy<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  deleteLegacy<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  post<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  put<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  patch<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  delete<T>(path: string, options?: ApiBodyRequestOptions): Promise<T>;
  download(path: string, options?: ApiRequestOptions): Promise<Response>;
}

function isEnvelope<T>(body: unknown): body is ApiEnvelope<T> {
  if (!body || typeof body !== "object") return false;
  const value = body as Partial<ApiEnvelope<T>>;
  return typeof value.success === "boolean" && typeof value.traceId === "string";
}

export function createApiClient(): ApiClient {
  async function authorizedHeaders(extra?: Readonly<Record<string, string>>) {
    const headers: Record<string, string> = { ...extra };
    if (typeof document !== "undefined") {
      headers["accept-language"] = document.documentElement.lang || "bs";
    }
    const accessToken = await getAccessToken();
    if (accessToken) headers["authorization"] = `Bearer ${accessToken}`;
    return headers;
  }

  async function request<T>(
    method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE",
    path: string,
    options: ApiBodyRequestOptions = {},
    envelopeRequired = true,
  ): Promise<T> {
    const controller = new AbortController();
    const abort = () => controller.abort(options.signal?.reason);
    const timeout = setTimeout(
      () => controller.abort(new Error("Request timed out")),
      options.timeoutMs ?? REQUEST_TIMEOUT_MS,
    );
    options.signal?.addEventListener("abort", abort, { once: true });

    try {
      const isFormData = options.body instanceof FormData;
      const headers = await authorizedHeaders({ accept: "application/json", ...options.headers });
      if (options.body !== undefined && !isFormData) headers["content-type"] = "application/json";
      const requestBody =
        options.body === undefined
          ? undefined
          : isFormData
            ? options.body
            : JSON.stringify(options.body);
      const init: RequestInit = {
        method,
        credentials: "include",
        headers,
        signal: controller.signal,
      };
      if (requestBody !== undefined) init.body = requestBody;
      const response = await fetch(buildUrl(path, options.query), init);

      const body: unknown =
        response.status === 204 ? null : await response.json().catch(() => null);
      if (!envelopeRequired) {
        if (!response.ok) {
          const legacyFailure = extractFailure(body, response.status);
          throw new ApiError(
            response.status,
            legacyFailure,
            response.headers.get("x-correlation-id") ?? "unknown",
          );
        }
        return body as T;
      }
      if (!isEnvelope<T>(body)) {
        throw new ApiError(
          response.status,
          { code: "invalid_response", message: "The server returned an invalid response." },
          response.headers.get("x-correlation-id") ?? "unknown",
        );
      }
      if (!response.ok || !body.success || body.data === null) {
        throw new ApiError(
          response.status,
          body.error ?? { code: "request_failed", message: "The request could not be completed." },
          body.traceId,
        );
      }
      return body.data;
    } finally {
      clearTimeout(timeout);
      options.signal?.removeEventListener("abort", abort);
    }
  }

  return {
    get: (path, options) => request("GET", path, options),
    getLegacy: (path, options) => request("GET", path, options, false),
    postLegacy: (path, options) => request("POST", path, options, false),
    putLegacy: (path, options) => request("PUT", path, options, false),
    deleteLegacy: (path, options) => request("DELETE", path, options, false),
    post: (path, options) => request("POST", path, options),
    put: (path, options) => request("PUT", path, options),
    patch: (path, options) => request("PATCH", path, options),
    delete: (path, options) => request("DELETE", path, options),
    download: async (path, options = {}) => {
      const controller = new AbortController();
      const abort = () => controller.abort(options.signal?.reason);
      const timeout = setTimeout(
        () => controller.abort(new Error("Request timed out")),
        options.timeoutMs ?? REQUEST_TIMEOUT_MS,
      );
      options.signal?.addEventListener("abort", abort, { once: true });
      try {
        const response = await fetch(buildUrl(path, options.query), {
          method: "GET",
          credentials: "include",
          headers: await authorizedHeaders({ accept: "application/octet-stream", ...options.headers }),
          signal: controller.signal,
        });
        if (!response.ok) {
          const body: unknown = await response.clone().json().catch(() => null);
          throw new ApiError(
            response.status,
            extractFailure(body, response.status),
            response.headers.get("x-correlation-id") ?? "unknown",
          );
        }
        return response;
      } finally {
        clearTimeout(timeout);
        options.signal?.removeEventListener("abort", abort);
      }
    },
  };
}

function extractFailure(body: unknown, status: number): ApiFailure {
  if (body && typeof body === "object") {
    const value = body as Record<string, unknown>;
    const message = [value["detail"], value["message"], value["title"], value["error"]]
      .find((candidate): candidate is string => typeof candidate === "string" && candidate.trim().length > 0);
    const errors = value["errors"];
    const details = extractDetails(errors);
    if (message || details) {
      return {
        code: String(value["code"] ?? `http_${status}`),
        message: message ?? Object.values(details ?? {}).flat().join(" "),
        ...(details ? { details } : {}),
      };
    }
  }
  return { code: `http_${status}`, message: `Zahtjev nije uspio (HTTP ${status}).` };
}

function extractDetails(errors: unknown): Record<string, string[]> | undefined {
  if (Array.isArray(errors)) {
    const grouped: Record<string, string[]> = {};
    for (const item of errors) {
      if (!item || typeof item !== "object") continue;
      const error = item as Record<string, unknown>;
      const field = typeof error["field"] === "string" && error["field"] ? error["field"] : "general";
      const message = typeof error["message"] === "string" ? error["message"] : undefined;
      if (message) (grouped[field] ??= []).push(message);
    }
    return Object.keys(grouped).length ? grouped : undefined;
  }
  if (!errors || typeof errors !== "object") return undefined;
  return Object.fromEntries(Object.entries(errors as Record<string, unknown>).map(([key, item]) => [
    key,
    Array.isArray(item) ? item.map(String) : [String(item)],
  ]));
}

export const apiClient: ApiClient = createApiClient();

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) return error instanceof Error && error.message ? error.message : fallback;
  const details = Object.entries(error.details ?? {})
    .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`));
  const systemMessage = /JSON value could not be converted|dto field is required|System\.|BytePositionInLine|LineNumber/i.test(error.message)
    ? ""
    : error.message;
  const parts = [systemMessage, ...details].filter((part) => Boolean(part) && !/JSON value could not be converted|System\.|BytePositionInLine|LineNumber/i.test(part));
  if (error.traceId && error.traceId !== "unknown" && error.traceId !== "legacy-boundary")
    parts.push(`ID: ${error.traceId}`);
  return parts.join(" ") || fallback;
}

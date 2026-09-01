import { useTranslation } from "react-i18next";

/**
 * Typed backend error-code localization.
 *
 * The API returns stable machine-readable codes; the frontend owns the wording.
 * Codes are mapped through an explicit allowlist so a hostile or unexpected
 * server value can never construct an arbitrary translation key.
 */

export const backendErrorCodes = [
  "COUNTRY_PROVIDER_UNAVAILABLE",
  "ACCOUNT_PROVIDER_UNAVAILABLE",
  "ACCOUNT_NOT_FOUND",
  "VALIDATION_FAILED",
  "UNAUTHORIZED",
  "FORBIDDEN",
  "RATE_LIMITED",
  "REQUEST_TIMEOUT",
  "INTERNAL_ERROR",
] as const;

export type BackendErrorCode = (typeof backendErrorCodes)[number];

const allowlist = new Set<string>(backendErrorCodes);

export const genericErrorKey = "errors.GENERIC" as const;

export function isKnownBackendErrorCode(code: unknown): code is BackendErrorCode {
  return typeof code === "string" && allowlist.has(code);
}

/** Translation key for a backend code, or the safe generic key. */
export function errorTranslationKey(code: unknown): string {
  return isKnownBackendErrorCode(code) ? `errors.${code}` : genericErrorKey;
}

export interface LocalizedApiError {
  readonly code: string | undefined;
  readonly message: string;
  readonly isKnown: boolean;
}

/** Hook form: translate an API problem document into user-facing wording. */
export function useLocalizedApiError() {
  const { t } = useTranslation("errors");

  return (problem: { code?: string | undefined; status?: number } | undefined): LocalizedApiError => {
    const code = problem?.code;
    const known = isKnownBackendErrorCode(code);
    const key = errorTranslationKey(code).replace(/^errors\./, "");
    return {
      code,
      isKnown: known,
      message: t(key, { defaultValue: t("GENERIC") }),
    };
  };
}

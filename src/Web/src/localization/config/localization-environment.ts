import { z } from "zod";
import { runtimeConfig } from "../../runtime-config";

/**
 * Localization environment configuration.
 *
 * Every knob is runtime configuration so the delivery target can be changed per
 * environment without touching code. Nothing here is secret: Phrase and Azure
 * publication credentials live only in CI, never in the browser bundle.
 */

const booleanish = z
  .union([z.boolean(), z.string()])
  .transform((value) => (typeof value === "boolean" ? value : value.trim().toLowerCase() === "true"));

const positiveMs = z
  .union([z.number(), z.string()])
  .transform((value) => (typeof value === "number" ? value : Number.parseInt(value, 10)))
  .refine((value) => Number.isFinite(value) && value > 0, "Must be a positive number of milliseconds");

const environmentSchema = z.object({
  /** Absolute or root-relative URL of the ACTIVE environment manifest. */
  manifestUrl: z.string().min(1),
  requestTimeoutMs: positiveMs.default(10_000),
  manifestRevalidationMs: positiveMs.default(60_000),
  enablePseudoLocale: booleanish.default(false),
  debug: booleanish.default(false),
});

export type LocalizationEnvironment = z.infer<typeof environmentSchema> & {
  /** True only in a development build; production must never pseudo-localize. */
  readonly isDevelopment: boolean;
};

const DEFAULT_MANIFEST_URL = "/localization/manifests/development.json";

function readEnv(): Record<string, unknown> {
  const raw = {
    manifestUrl: runtimeConfig.LOCALIZATION_MANIFEST_URL?.trim() || DEFAULT_MANIFEST_URL,
    requestTimeoutMs: runtimeConfig.LOCALIZATION_REQUEST_TIMEOUT_MS,
    manifestRevalidationMs: runtimeConfig.LOCALIZATION_MANIFEST_REVALIDATION_MS,
    enablePseudoLocale: runtimeConfig.ENABLE_PSEUDO_LOCALE,
    debug: runtimeConfig.LOCALIZATION_DEBUG,
  };
  // Drop unset values so Zod defaults apply instead of failing on "".
  return Object.fromEntries(Object.entries(raw).filter(([, value]) => value !== undefined && value !== ""));
}

/**
 * Validate configuration once at startup. A misconfigured environment is a
 * deployment error and must surface loudly rather than silently degrade.
 */
export function readLocalizationEnvironment(): LocalizationEnvironment {
  const parsed = environmentSchema.safeParse(readEnv());
  if (!parsed.success) {
    const details = parsed.error.issues.map((i) => `${i.path.join(".")}: ${i.message}`).join("; ");
    throw new Error(`Invalid localization environment configuration — ${details}`);
  }
  const isDevelopment = runtimeConfig.ENVIRONMENT?.toLowerCase() === "development";
  return {
    ...parsed.data,
    // Hard guarantee: pseudo-localization can never ship to production.
    enablePseudoLocale: parsed.data.enablePseudoLocale && isDevelopment,
    isDevelopment,
  };
}

export const localizationEnvironment: LocalizationEnvironment = readLocalizationEnvironment();

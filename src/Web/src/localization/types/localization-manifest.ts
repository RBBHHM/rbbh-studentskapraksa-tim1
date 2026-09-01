import { z } from "zod";

/**
 * Runtime contract between the localization delivery platform (Azure Blob
 * Storage behind Front Door / a CDN) and this application.
 *
 * The manifest is the ONLY mutable localization artifact: it points at an
 * immutable, versioned release. Publishing, promoting and rolling back
 * translations therefore never requires a frontend or API deployment.
 *
 * Everything is validated with Zod at runtime — a malformed manifest must be
 * rejected safely rather than poisoning the last-known-good cache.
 */

/** Loose BCP 47 shape check; exact validity is confirmed via `Intl.Locale`. */
const bcp47 = z
  .string()
  .min(2)
  .max(35)
  .regex(/^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$/, "Locale must be a valid BCP 47 language tag");

export const textDirectionSchema = z.enum(["ltr", "rtl"]);
export type TextDirection = z.infer<typeof textDirectionSchema>;

export const supportedLocaleSchema = z.object({
  code: bcp47,
  /** Key inside the `common` namespace, e.g. `locales.de`. */
  displayNameKey: z.string().min(1),
  direction: textDirectionSchema,
  enabled: z.boolean(),
});
export type SupportedLocale = z.infer<typeof supportedLocaleSchema>;

/** Namespaces are file names inside a release, so keep them path-safe. */
export const namespaceSchema = z
  .string()
  .min(1)
  .max(64)
  .regex(/^[a-z0-9]+(-[a-z0-9]+)*$/, "Namespace must be lowercase kebab-case");
export type LocalizationNamespace = z.infer<typeof namespaceSchema>;

/** Release ids become URL path segments and must never contain traversal. */
export const releaseIdSchema = z
  .string()
  .min(1)
  .max(64)
  .regex(/^[A-Za-z0-9][A-Za-z0-9._-]*$/, "Release id must be a safe path segment");

export const localizationManifestSchema = z
  .object({
    schemaVersion: z.literal(1),
    releaseId: releaseIdSchema,
    defaultLocale: bcp47,
    fallbackLocale: bcp47,
    supportedLocales: z.array(supportedLocaleSchema).min(1),
    namespaces: z.array(namespaceSchema).min(1),
    publishedAt: z.string().datetime({ offset: true }),
  })
  .strict()
  .superRefine((manifest, ctx) => {
    const codes = new Set(manifest.supportedLocales.map((entry) => entry.code));
    if (codes.size !== manifest.supportedLocales.length) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Duplicate locale codes" });
    }
    for (const required of [manifest.defaultLocale, manifest.fallbackLocale]) {
      if (!codes.has(required)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: `Locale "${required}" is not present in supportedLocales`,
        });
      }
    }
    if (new Set(manifest.namespaces).size !== manifest.namespaces.length) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Duplicate namespaces" });
    }
  });

export type LocalizationManifest = z.infer<typeof localizationManifestSchema>;

export interface ManifestValidationFailure {
  readonly ok: false;
  readonly issues: readonly string[];
}

export interface ManifestValidationSuccess {
  readonly ok: true;
  readonly manifest: LocalizationManifest;
}

export type ManifestValidationResult = ManifestValidationSuccess | ManifestValidationFailure;

/** Validate an unknown payload without throwing, so callers can fall back. */
export function validateManifest(payload: unknown): ManifestValidationResult {
  const parsed = localizationManifestSchema.safeParse(payload);
  if (parsed.success) return { ok: true, manifest: parsed.data };
  return {
    ok: false,
    issues: parsed.error.issues.map((issue) => `${issue.path.join(".") || "<root>"}: ${issue.message}`),
  };
}

/** Locales a switcher may offer: present in the manifest AND enabled. */
export function enabledLocales(manifest: LocalizationManifest): readonly SupportedLocale[] {
  return manifest.supportedLocales.filter((entry) => entry.enabled);
}

export function localeEntry(
  manifest: LocalizationManifest,
  code: string,
): SupportedLocale | undefined {
  return manifest.supportedLocales.find((entry) => entry.code === code);
}

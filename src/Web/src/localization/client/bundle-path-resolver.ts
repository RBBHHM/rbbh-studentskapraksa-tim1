import { releaseIdSchema } from "../types/localization-manifest";

/**
 * Resolves URLs inside the localization delivery layout.
 *
 * Layout (immutable after publication):
 *   /localization/manifests/<environment>.json
 *   /localization/releases/<releaseId>/<locale>/<namespace>.json
 *
 * Bundle URLs are versioned by release id, which is why they can be cached
 * `immutable` for a year: a translation correction produces a NEW release id
 * rather than overwriting a published file. No cache-busting query strings.
 */

/** Derive the delivery root from the configured manifest URL. */
export function localizationRootFromManifestUrl(manifestUrl: string): string {
  const marker = "/manifests/";
  const index = manifestUrl.indexOf(marker);
  if (index === -1) {
    throw new Error(
      `LOCALIZATION_MANIFEST_URL must contain "/manifests/" — received "${manifestUrl}"`,
    );
  }
  return manifestUrl.slice(0, index);
}

function safeSegment(value: string, label: string): string {
  if (!/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(value)) {
    throw new Error(`Unsafe ${label} segment: "${value}"`);
  }
  return value;
}

export interface BundlePathInput {
  readonly manifestUrl: string;
  readonly releaseId: string;
  readonly locale: string;
  readonly namespace: string;
}

export function resolveBundleUrl({
  manifestUrl,
  releaseId,
  locale,
  namespace,
}: BundlePathInput): string {
  const release = releaseIdSchema.parse(releaseId);
  const root = localizationRootFromManifestUrl(manifestUrl);
  return `${root}/releases/${release}/${safeSegment(locale, "locale")}/${safeSegment(namespace, "namespace")}.json`;
}

/** i18next `loadPath` template for the active release. */
export function bundleLoadPathTemplate(manifestUrl: string, releaseId: string): string {
  const release = releaseIdSchema.parse(releaseId);
  return `${localizationRootFromManifestUrl(manifestUrl)}/releases/${release}/{{lng}}/{{ns}}.json`;
}

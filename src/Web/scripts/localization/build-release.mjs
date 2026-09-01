#!/usr/bin/env node
/**
 * Localization release builder (export pipeline stage).
 *
 * Reads reviewed source bundles from `src/localization/source/<locale>/<ns>.json`
 * — in a full setup these are exported from the translation management system
 * after approval — and writes an IMMUTABLE release plus the environment
 * manifest that points at it:
 *
 *   public/localization/releases/<releaseId>/<locale>/<ns>.json
 *   public/localization/manifests/<environment>.json
 *
 * In production the same output is uploaded to Azure Blob Storage and served
 * through Front Door. Publishing or rolling back is a manifest write, never an
 * application deployment.
 *
 * Usage:
 *   node scripts/localization/build-release.mjs [--environment development] [--release 2026.08.06-001]
 */

import { mkdir, readdir, readFile, writeFile, rm } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const sourceRoot = join(root, "src/localization/source");
const publicRoot = join(root, "public/localization");

/** Locale policy lives with the release, so a new language is data, not code. */
const localePolicy = {
  en: { direction: "ltr", enabled: true },
  // German bundles remain available for future review, but the application is
  // intentionally offered in Bosnian and English until every business term is approved.
  de: { direction: "ltr", enabled: false },
  bs: { direction: "ltr", enabled: true },
};

const defaultLocale = "en";
const fallbackLocale = "en";

function arg(name, fallback) {
  const index = process.argv.indexOf(`--${name}`);
  return index !== -1 && process.argv[index + 1] ? process.argv[index + 1] : fallback;
}

function defaultReleaseId() {
  const now = new Date();
  const date = now.toISOString().slice(0, 10).replaceAll("-", ".");
  return `${date}-001`;
}

async function readLocale(locale) {
  const dir = join(sourceRoot, locale);
  const files = (await readdir(dir)).filter((name) => name.endsWith(".json"));
  const bundles = new Map();
  for (const file of files) {
    const namespace = file.replace(/\.json$/, "");
    bundles.set(namespace, JSON.parse(await readFile(join(dir, file), "utf8")));
  }
  return bundles;
}

function flatten(value, prefix = "", out = new Set()) {
  for (const [key, entry] of Object.entries(value)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (entry && typeof entry === "object" && !Array.isArray(entry)) flatten(entry, path, out);
    else out.add(path);
  }
  return out;
}

async function main() {
  const environment = arg("environment", "development");
  const releaseId = arg("release", defaultReleaseId());

  const locales = (await readdir(sourceRoot, { withFileTypes: true }))
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .sort();

  const sourceBundles = await readLocale(defaultLocale);
  const namespaces = [...sourceBundles.keys()].sort();
  const sourceKeys = new Map(
    namespaces.map((namespace) => [namespace, flatten(sourceBundles.get(namespace))]),
  );

  const releaseDir = join(publicRoot, "releases", releaseId);
  await rm(releaseDir, { recursive: true, force: true });

  const problems = [];

  for (const locale of locales) {
    if (!localePolicy[locale]) {
      problems.push(`Locale "${locale}" has source bundles but no entry in localePolicy.`);
      continue;
    }
    const bundles = await readLocale(locale);
    for (const namespace of namespaces) {
      const bundle = bundles.get(namespace);
      if (!bundle) {
        problems.push(`${locale}: missing namespace "${namespace}".`);
        continue;
      }
      const keys = flatten(bundle);
      for (const key of sourceKeys.get(namespace)) {
        if (!keys.has(key)) problems.push(`${locale}/${namespace}: missing key "${key}".`);
      }
      for (const key of keys) {
        if (!sourceKeys.get(namespace).has(key)) {
          problems.push(`${locale}/${namespace}: unknown key "${key}" (not in ${defaultLocale}).`);
        }
      }
      const target = join(releaseDir, locale, `${namespace}.json`);
      await mkdir(dirname(target), { recursive: true });
      await writeFile(target, `${JSON.stringify(bundle, null, 2)}\n`, "utf8");
    }
  }

  if (problems.length > 0) {
    console.error("Localization release rejected:\n" + problems.map((p) => `  - ${p}`).join("\n"));
    process.exit(1);
  }

  const manifest = {
    schemaVersion: 1,
    releaseId,
    defaultLocale,
    fallbackLocale,
    supportedLocales: locales.map((code) => ({
      code,
      displayNameKey: `locales.${code}`,
      direction: localePolicy[code].direction,
      enabled: localePolicy[code].enabled,
    })),
    namespaces,
    publishedAt: new Date().toISOString(),
  };

  const manifestPath = join(publicRoot, "manifests", `${environment}.json`);
  await mkdir(dirname(manifestPath), { recursive: true });
  await writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");

  console.info(
    `Localization release ${releaseId} written for ${locales.length} locales and ${namespaces.length} namespaces.`,
  );
  console.info(`Manifest: public/localization/manifests/${environment}.json`);
}

await main();

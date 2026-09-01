#!/usr/bin/env node
/**
 * CI guard: translation key hygiene.
 *
 * Fails the build when
 *   1. a locale bundle is missing a key present in the source locale,
 *   2. a locale bundle contains a key the source locale does not define,
 *   3. an ICU message has unbalanced braces,
 *   4. a `t("…")` call in `src/` references a key that does not exist.
 *
 * This is what keeps "translations are runtime content" from turning into
 * "translations are unverified content".
 */

import { readdir, readFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const sourceRoot = join(root, "src/localization/source");
const appRoot = join(root, "src");
const SOURCE_LOCALE = "en";

function flatten(value, prefix = "", out = new Map()) {
  for (const [key, entry] of Object.entries(value)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (entry && typeof entry === "object" && !Array.isArray(entry)) flatten(entry, path, out);
    else out.set(path, entry);
  }
  return out;
}

function bracesBalanced(message) {
  let depth = 0;
  for (const char of message) {
    if (char === "{") depth += 1;
    if (char === "}") depth -= 1;
    if (depth < 0) return false;
  }
  return depth === 0;
}

async function readLocaleKeys(locale) {
  const dir = join(sourceRoot, locale);
  const namespaces = new Map();
  for (const file of (await readdir(dir)).filter((name) => name.endsWith(".json"))) {
    const namespace = file.replace(/\.json$/, "");
    namespaces.set(namespace, flatten(JSON.parse(await readFile(join(dir, file), "utf8"))));
  }
  return namespaces;
}

async function collectSourceFiles(dir, files = []) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    if (entry.name === "source" || entry.name === "node_modules") continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) await collectSourceFiles(full, files);
    else if (/\.(ts|tsx)$/.test(entry.name)) files.push(full);
  }
  return files;
}

const problems = [];

const locales = (await readdir(sourceRoot, { withFileTypes: true }))
  .filter((entry) => entry.isDirectory())
  .map((entry) => entry.name);

const sourceKeys = await readLocaleKeys(SOURCE_LOCALE);

for (const locale of locales) {
  const bundles = await readLocaleKeys(locale);
  for (const [namespace, keys] of sourceKeys) {
    const target = bundles.get(namespace);
    if (!target) {
      problems.push(`${locale}: missing namespace "${namespace}"`);
      continue;
    }
    for (const key of keys.keys()) {
      if (!target.has(key)) problems.push(`${locale}/${namespace}: missing key "${key}"`);
    }
    for (const [key, message] of target) {
      if (!keys.has(key)) problems.push(`${locale}/${namespace}: unknown key "${key}"`);
      if (typeof message === "string" && !bracesBalanced(message)) {
        problems.push(`${locale}/${namespace}: unbalanced ICU braces in "${key}"`);
      }
    }
  }
}

// Static t("key") usage check. Only literal calls are verifiable; dynamic keys
// must go through an explicit allowlist in code (see error-code-map.ts).
const knownKeys = new Set();
for (const [namespace, keys] of sourceKeys) {
  for (const key of keys.keys()) {
    knownKeys.add(`${namespace}:${key}`);
    if (namespace === "common") knownKeys.add(key);
  }
}

const callPattern = /\bt\(\s*"([a-zA-Z0-9_.:-]+)"/g;
for (const file of await collectSourceFiles(appRoot)) {
  const content = await readFile(file, "utf8");
  const namespaceHints = [...content.matchAll(/useTranslation\(\s*(\[[^\]]*\]|"[^"]+")/g)]
    .flatMap((match) => [...match[1].matchAll(/"([^"]+)"/g)].map((m) => m[1]));
  for (const match of content.matchAll(callPattern)) {
    const key = match[1];
    if (key.includes(":")) {
      if (!knownKeys.has(key)) problems.push(`${file}: unknown translation key "${key}"`);
      continue;
    }
    const candidates = namespaceHints.length > 0 ? namespaceHints : ["common"];
    if (!candidates.some((namespace) => knownKeys.has(`${namespace}:${key}`))) {
      problems.push(`${file}: unknown translation key "${key}" in [${candidates.join(", ")}]`);
    }
  }
}

if (problems.length > 0) {
  console.error("Localization key check failed:\n" + problems.map((p) => `  - ${p}`).join("\n"));
  process.exit(1);
}

console.info(`Localization key check passed for ${locales.length} locales.`);

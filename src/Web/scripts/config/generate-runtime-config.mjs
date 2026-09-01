import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const outputDirectory = resolve(process.argv[2] ?? ".runtime");
const candidates = [resolve(".env"), resolve("..", "..", ".env")];
const source = candidates.find(existsSync);
const values = {};

if (source) {
  for (const line of readFileSync(source, "utf8").split(/\r?\n/u)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) continue;
    const separator = trimmed.indexOf("=");
    if (separator < 1) continue;
    const key = trimmed.slice(0, separator).trim();
    let value = trimmed.slice(separator + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    values[key] = value;
  }
}

const mapping = {
  API_BASE_URL: "API_BASE_URL",
  KEYCLOAK_URL: "KEYCLOAK_URL",
  KEYCLOAK_REALM: "KEYCLOAK_REALM",
  KEYCLOAK_CLIENT_ID: "KEYCLOAK_CLIENT_ID",
  LOCALIZATION_MANIFEST_URL: "LOCALIZATION_MANIFEST_URL",
  LOCALIZATION_REQUEST_TIMEOUT_MS: "LOCALIZATION_REQUEST_TIMEOUT_MS",
  LOCALIZATION_MANIFEST_REVALIDATION_MS: "LOCALIZATION_MANIFEST_REVALIDATION_MS",
  ENABLE_PSEUDO_LOCALE: "ENABLE_PSEUDO_LOCALE",
  LOCALIZATION_DEBUG: "LOCALIZATION_DEBUG",
  ENVIRONMENT: "APP_ENVIRONMENT",
};

const config = Object.fromEntries(
  Object.entries(mapping).map(([target, sourceKey]) => [target, values[sourceKey] ?? process.env[sourceKey] ?? ""]),
);
mkdirSync(outputDirectory, { recursive: true });
writeFileSync(resolve(outputDirectory, "app-config.js"), `window.__APP_CONFIG__ = ${JSON.stringify(config, null, 2)};\n`, "utf8");

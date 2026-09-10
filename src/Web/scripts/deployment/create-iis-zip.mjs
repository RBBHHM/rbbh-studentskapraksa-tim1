import { mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { zipSync } from "fflate";

const source = resolve("dist");
const output = resolve("artifacts", "connected-parties-iis.zip");
const files = {};
const backendOriginIndex = process.argv.indexOf("--backend-origin");
const backendOriginValue = backendOriginIndex >= 0 ? process.argv[backendOriginIndex + 1] : process.env.OCP_BACKEND_ORIGIN;
const keycloakOriginIndex = process.argv.indexOf("--keycloak-origin");
const keycloakOriginValue = keycloakOriginIndex >= 0 ? process.argv[keycloakOriginIndex + 1] : process.env.KEYCLOAK_PUBLIC_ORIGIN;

if (!backendOriginValue) throw new Error("Provide --backend-origin or OCP_BACKEND_ORIGIN.");
if (!keycloakOriginValue) throw new Error("Provide --keycloak-origin or KEYCLOAK_PUBLIC_ORIGIN.");
const backendOriginUrl = new URL(backendOriginValue);
if (!["http:", "https:"].includes(backendOriginUrl.protocol) || backendOriginUrl.search || backendOriginUrl.hash) {
  throw new Error("Backend origin must be an HTTP(S) URL without query or fragment.");
}
const keycloakOriginUrl = new URL(keycloakOriginValue);
if (!["http:", "https:"].includes(keycloakOriginUrl.protocol) || keycloakOriginUrl.search || keycloakOriginUrl.hash) {
  throw new Error("Keycloak origin must be an HTTP(S) URL without query or fragment.");
}
const backendOrigin = backendOriginValue.replace(/\/+$/u, "");
const keycloakOrigin = keycloakOriginValue.replace(/\/+$/u, "");

function collect(directory) {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) collect(path);
    else files[relative(source, path).replaceAll("\\", "/")] = readFileSync(path);
  }
}

collect(source);
const webConfig = readFileSync(resolve("deploy", "iis", "web.config"), "utf8")
  .replaceAll("__BACKEND_ORIGIN__", backendOrigin)
  .replaceAll("__KEYCLOAK_ORIGIN__", keycloakOrigin);
if (webConfig.includes("__BACKEND_ORIGIN__") || webConfig.includes("__KEYCLOAK_ORIGIN__")) {
  throw new Error("Deployment origin placeholder was not replaced.");
}
files["web.config"] = Buffer.from(webConfig, "utf8");
mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, zipSync(files, { level: 9 }));
console.log(`IIS artifact: ${output}`);

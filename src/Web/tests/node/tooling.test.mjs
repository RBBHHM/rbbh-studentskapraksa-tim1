import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const packageJson = JSON.parse(readFileSync(new URL("../../package.json", import.meta.url), "utf8"));
const webConfig = readFileSync(new URL("../../deploy/iis/web.config", import.meta.url), "utf8");
const runtimeGenerator = readFileSync(new URL("../../scripts/config/generate-runtime-config.mjs", import.meta.url), "utf8");
const iisPackager = readFileSync(new URL("../../scripts/deployment/create-iis-zip.mjs", import.meta.url), "utf8");

test("supported frontend path contains no blocked native toolchain", () => {
  const all = JSON.stringify(packageJson);
  for (const forbidden of ["vite", "vitest", "esbuild", "playwright", "storybook", "@tanstack/react-start"]) {
    assert.equal(all.includes(forbidden), false, `${forbidden} must not be part of supported tooling`);
  }
});

test("development commands use Windows-safe pnpm wrapper", () => {
  assert.match(packageJson.scripts.dev, /pnpm\.cmd/u);
  assert.match(packageJson.scripts["publish:iis"], /pnpm\.cmd/u);
});

test("IIS package proxies backend and serves localization and fonts", () => {
  assert.match(webConfig, /<add value="index\.html" \/>/u);
  assert.match(webConfig, /<action type="Rewrite" url="\/index\.html" \/>/u);
  assert.doesNotMatch(webConfig, /_shell\.html/u);
  assert.match(webConfig, /name="OCP backend reverse proxy"/u);
  for (const route of ["api", "authentication", "health", "signin-oidc", "signout-callback-oidc"]) {
    assert.match(webConfig, new RegExp(`\\b${route}\\b`, "u"));
  }
  assert.match(webConfig, /HTTP_X_FORWARDED_HOST/u);
  assert.match(webConfig, /HTTP_X_FORWARDED_PROTO/u);
  assert.match(webConfig, /fileExtension="\.json" mimeType="application\/json"/u);
  assert.doesNotMatch(webConfig, /application\/json; charset=/u);
  assert.match(webConfig, /fileExtension="\.woff2" mimeType="font\/woff2"/u);
  assert.match(iisPackager, /--backend-origin/u);
});

test("deployment config overrides local environment and uses same-origin API", () => {
  assert.match(runtimeGenerator, /process\.env\[sourceKey\] \?\? values\[sourceKey\]/u);
  assert.match(runtimeGenerator, /--same-origin/u);
  assert.match(packageJson.scripts["publish:iis"], /--environment production/u);
  assert.match(packageJson.scripts["publish:iis"], /\/localization\/manifests\/production\.json/u);
});

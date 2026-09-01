import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const packageJson = JSON.parse(readFileSync(new URL("../../package.json", import.meta.url), "utf8"));

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

import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const routes = ["users", "audit", "code-lists", "period"];

test("every administration route blocks direct navigation without admin permission", () => {
  for (const route of routes) {
    const source = readFileSync(
      new URL(`../../src/routes/app.admin.${route}.tsx`, import.meta.url),
      "utf8",
    );
    assert.match(source, /beforeLoad:\s*requireApplicationAdmin/u, route);
  }
});

test("administration guard is based on the backend permission claim", () => {
  const source = readFileSync(
    new URL("../../src/lib/auth/application-access.ts", import.meta.url),
    "utf8",
  );
  assert.match(source, /hasPermission\("ADMINISTRATION_MANAGE"\)/u);
  assert.match(source, /throw redirect\(\{ to: "\/app" \}\)/u);
});
import assert from "node:assert/strict";
import { readFileSync, readdirSync } from "node:fs";
import test from "node:test";

const read = (path) => readFileSync(new URL(`../../${path}`, import.meta.url), "utf8");

test("styles are emitted and sticky chrome is opaque", () => {
  const packageJson = JSON.parse(read("package.json"));
  const styles = read("src/styles.css");
  assert.deepEqual(packageJson.sideEffects, ["**/*.css"]);
  assert.doesNotMatch(styles, /@utility\s/);
  assert.match(styles, /\.glass-strong\s*\{[^}]*background-color:\s*var\(--surface-default\)[^}]*backdrop-filter:\s*none/s);
});

test("shared UI contains no Tailwind 4-only variable shorthand", () => {
  const uiRoot = new URL("../../src/components/ui/", import.meta.url);
  for (const entry of readdirSync(uiRoot, { recursive: true })) {
    if (!String(entry).endsWith(".tsx")) continue;
    const source = readFileSync(new URL(String(entry).replaceAll("\\", "/"), uiRoot), "utf8");
    assert.doesNotMatch(source, /[a-z-]+-\(--/);
  }
});

test("approved RBI logo files and theme variants are present", () => {
  const assets = read("src/design-system/foundations/logo-assets.ts");
  const shell = read("src/components/layout/app-shell.tsx");
  assert.match(assets, /bankMono:[\s\S]*rbi-bank-mono-pos\.png/);
  assert.match(assets, /bankYellowInverse:[\s\S]*rbi-bank-yellow-neg\.png/);
  assert.match(shell, /resolvedTheme === "dark" \? "bankYellowInverse" : "bankMono"/);
});

test("registry header switches between approved light and dark logo assets", () => {
  const shell = read("src/components/registry/registry-shell.tsx");
  assert.match(shell, /variant="bankMono".*dark:hidden/s);
  assert.match(shell, /variant="bankYellowInverse".*dark:block/s);
});

test("shared form and dialog surfaces are never transparent", () => {
  const languageSwitcher = read("src/localization/components/language-switcher.tsx");
  assert.doesNotMatch(languageSwitcher, /border-border-subtle bg-transparent/);
  for (const file of ["dialog.tsx", "alert-dialog.tsx"]) {
    const source = read(`src/components/ui/${file}`);
    assert.match(source, /bg-surface-raised/);
  }
  for (const file of ["input.tsx", "textarea.tsx", "select.tsx"]) {
    try {
      const source = read(`src/components/ui/${file}`);
      assert.match(source, /bg-surface-raised/);
    } catch (error) {
      if (error?.code !== "ENOENT") throw error;
    }
  }
});

test("registry uses icon indicators and the correct physical-person mutation endpoint", () => {
  const resourcePage = read("src/components/registry/resource-page.tsx");
  const resources = read("src/lib/registry/resources.ts");
  const indicator = read("src/components/registry/icon-indicator.tsx");
  assert.match(resources, /key:\s*"physicalPersons"[\s\S]*mutationEndpoint:\s*"\/api\/related-persons"/);
  assert.match(resourcePage, /resource\.capabilities\?\.mutationEndpoint \?\? resource\.endpoint/);
  assert.match(resourcePage, /<IconIndicator/);
  assert.match(indicator, /aria-label=\{label\}/);
  assert.match(indicator, /title=\{label\}/);
});

test("user guide is pinned to the sidebar and filters content by active access", () => {
  const shell = read("src/components/registry/registry-shell.tsx");
  const guide = read("src/components/registry/user-guide-page.tsx");
  assert.match(shell, /to="\/app\/guide"/);
  assert.match(guide, /activeApplicationAccesses\(\)/);
  assert.match(guide, /hasAllApplicationAccesses\(\)/);
  assert.match(guide, /sections\.filter/);
});

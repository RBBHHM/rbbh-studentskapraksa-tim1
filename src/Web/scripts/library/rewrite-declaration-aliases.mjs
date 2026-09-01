/**
 * Post-processes the emitted `.d.ts` files of @rbi/design-system.
 *
 * TypeScript keeps the `@/*` path alias verbatim in declaration output, which
 * consumers cannot resolve. This rewrites every alias import to a relative
 * specifier inside dist/ so the published types are self-contained.
 */
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";

import { globSync } from "tinyglobby";

const distDir = resolve(process.cwd(), "packages/rbi-design-system/dist");
const ALIAS = /(["'])@\/([^"']+)\1/g;

const files = globSync("**/*.d.ts", { cwd: distDir, absolute: true });
let rewritten = 0;

for (const file of files) {
  const source = readFileSync(file, "utf8");
  const next = source.replace(ALIAS, (_match, quote, target) => {
    const absolute = resolve(distDir, target);
    let specifier = relative(dirname(file), absolute).replaceAll("\\", "/");
    if (!specifier.startsWith(".")) specifier = `./${specifier}`;
    return `${quote}${specifier}${quote}`;
  });
  if (next !== source) {
    writeFileSync(file, next);
    rewritten += 1;
  }
}

console.log(`Rewrote alias imports in ${rewritten}/${files.length} declaration files.`);

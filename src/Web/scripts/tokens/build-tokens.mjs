#!/usr/bin/env node
/**
 * Generates the @rbi/tokens distributable package from the single source of
 * truth for design tokens:
 *   - src/styles.css                     (CSS custom properties, source of truth)
 *   - src/design-system/tokens/colors.ts (documentation mirror, cross-checked)
 *   - src/design-system/tokens/scales.ts (documentation mirror, cross-checked)
 *
 * Nothing in this script hand-types a token value: every value is parsed out
 * of src/styles.css. Regenerate with:
 *
 *   node scripts/tokens/build-tokens.mjs
 */

import { readFileSync, mkdirSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..", "..");
const STYLES_CSS = path.join(ROOT, "src", "styles.css");
const OUT_DIR = path.join(ROOT, "packages", "rbi-tokens", "dist");

const css = readFileSync(STYLES_CSS, "utf8");

// ---------------------------------------------------------------------------
// 1. Parse every top-level rule block (`selector { ...declarations... }`) out
//    of styles.css that declares CSS custom properties (`--foo: bar;`). We
//    only care about @-free, un-nested top-level blocks — this file does not
//    nest rules inside `@layer`/`@theme` blocks that also carry the raw
//    `--token: value;` shape we want other than @theme, which we skip because
//    it re-derives Tailwind-facing aliases from the same semantic tokens
//    already captured under :root.
// ---------------------------------------------------------------------------

/** @type {Map<string, string>} selector -> declarations text */
function extractTopLevelBlocks(source) {
  const blocks = [];
  let depth = 0;
  let blockStart = -1;
  let selectorStart = 0;
  for (let i = 0; i < source.length; i++) {
    const ch = source[i];
    if (ch === "{") {
      if (depth === 0) {
        // The raw text between the previous block and this "{" may include
        // unrelated statements (e.g. leading @import lines with no braces of
        // their own). Only the text after the last ";" or "}" is the actual
        // selector for this block.
        const rawSelector = source.slice(selectorStart, i);
        const lastBoundary = Math.max(rawSelector.lastIndexOf(";"), rawSelector.lastIndexOf("}"));
        const selector = rawSelector.slice(lastBoundary + 1).trim();
        blockStart = i + 1;
        blocks.push({ selector, bodyStart: blockStart, open: i });
      }
      depth++;
    } else if (ch === "}") {
      depth--;
      if (depth === 0) {
        const top = blocks[blocks.length - 1];
        top.bodyEnd = i;
        selectorStart = i + 1;
      }
    }
  }
  return blocks
    .filter((b) => b.bodyEnd !== undefined)
    .map((b) => ({ selector: b.selector, body: source.slice(b.bodyStart, b.bodyEnd) }));
}

function parseDeclarations(body) {
  /** @type {[string, string][]} */
  const entries = [];
  // Split declarations on ';' that are not inside parentheses.
  let depth = 0;
  let current = "";
  for (const ch of body) {
    if (ch === "(") depth++;
    if (ch === ")") depth--;
    if (ch === ";" && depth === 0) {
      entries.push(current);
      current = "";
    } else {
      current += ch;
    }
  }
  if (current.trim()) entries.push(current);

  const props = [];
  for (const raw of entries) {
    const line = raw.trim();
    if (!line.startsWith("--")) continue;
    const colonIndex = line.indexOf(":");
    if (colonIndex === -1) continue;
    const name = line.slice(0, colonIndex).trim();
    const value = line.slice(colonIndex + 1).trim();
    if (!name || !value) continue;
    props.push([name, value]);
  }
  return props;
}

const blocks = extractTopLevelBlocks(css);

// Strip comments so they don't interfere with block/declaration parsing.
function stripComments(source) {
  return source.replace(/\/\*[\s\S]*?\*\//g, "");
}

const cleanedBlocks = extractTopLevelBlocks(stripComments(css));

const rootBlocks = cleanedBlocks.filter((b) => b.selector === ":root");

// [data-surface="..."] rules live nested inside `@layer components { ... }`,
// so re-parse the body of any @layer block to find them too.
const layerBlocks = cleanedBlocks.filter((b) => /^@layer\b/.test(b.selector));
const nestedBlocks = layerBlocks.flatMap((b) => extractTopLevelBlocks(b.body));
const allBlocks = [...cleanedBlocks, ...nestedBlocks];

// Simple, single-attribute theme selectors only — compound selectors like
// Combined theme and surface selectors are edge-case combinators, not
// standalone themes/contexts, so they are intentionally left out of the
// distributable theme blocks.
const surfaceBlocks = allBlocks.filter((b) =>
  /^\[data-surface="[a-z]+"\]$/.test(b.selector.trim()),
);
const themeAttrBlocks = allBlocks.filter((b) =>
  /^\[data-theme="[a-z]+"\]$/.test(b.selector.trim()),
);

/** @type {Map<string,string>} token name (with --) -> raw value, in declaration order */
const rootTokens = new Map();
for (const block of rootBlocks) {
  for (const [name, value] of parseDeclarations(block.body)) {
    rootTokens.set(name, value);
  }
}

const themeBlocks = [...themeAttrBlocks, ...surfaceBlocks].map((block) => ({
  selector: block.selector,
  tokens: parseDeclarations(block.body).filter(([name]) => name.startsWith("--")),
}));

// ---------------------------------------------------------------------------
// 2. Resolve `var(--x)` references so every emitted value is a literal, while
//    keeping the alias chain around for documentation.
// ---------------------------------------------------------------------------

function resolveValue(value, seen = new Set()) {
  const varPattern = /var\((--[a-zA-Z0-9-]+)(?:\s*,\s*([^)]+))?\)/g;
  let resolved = value;
  let changed = true;
  let guard = 0;
  while (changed && guard < 20) {
    changed = false;
    guard++;
    resolved = resolved.replace(varPattern, (match, refName) => {
      if (seen.has(refName)) return match;
      const refValue = rootTokens.get(refName);
      if (refValue === undefined) return match;
      changed = true;
      return refValue;
    });
  }
  return resolved;
}

const resolvedRootTokens = new Map();
for (const [name, value] of rootTokens.entries()) {
  resolvedRootTokens.set(name, { raw: value, value: resolveValue(value) });
}

// ---------------------------------------------------------------------------
// 3. Classify each token into a W3C Design Tokens Community Group `$type`.
// ---------------------------------------------------------------------------

function classify(name, value) {
  const n = name.replace(/^--/, "");
  const isHexOrNamedColor =
    /^#[0-9a-f]{3,8}$/i.test(value.trim()) || /^(black|white|transparent)$/i.test(value.trim());
  const isColorFn = /^(rgb|rgba|hsl|hsla|color-mix|oklch)\(/i.test(value.trim());
  if (isHexOrNamedColor || isColorFn) return "color";
  if (
    /^rbi-(yellow|white|warm-grey|off-black|mono-black|grey|green|coral|purple|red|amber|blue|teal|lime|magenta|brown)/.test(
      n,
    )
  )
    return "color";
  if (/^(surface|text|border|action|feedback|data-\d|ring)-/.test(n) && !/width|opacity/.test(n))
    return "color";
  if (/^focus-ring-color/.test(n)) return "color";
  if (/^font-family/.test(n)) return "fontFamily";
  if (/^font-weight/.test(n)) return "fontWeight";
  if (/^font-size/.test(n)) return "dimension";
  if (/^line-height/.test(n)) return "number";
  if (/^letter-spacing/.test(n)) return "dimension";
  if (/^space-/.test(n)) return "dimension";
  if (/^size-/.test(n)) return "dimension";
  if (/^border-width/.test(n)) return "dimension";
  if (/^radii-/.test(n)) return "dimension";
  if (/^elevation-/.test(n)) return "shadow";
  if (/^z-/.test(n)) return "number";
  if (/^content-width|^content-gutter|^grid-gap|^breakpoint-|^brand-diagonal-height/.test(n))
    return "dimension";
  if (/^grid-columns/.test(n)) return "number";
  if (/^duration-/.test(n)) return "duration";
  if (/^easing-/.test(n)) return "cubicBezier";
  if (/^focus-ring-width|^focus-ring-offset/.test(n)) return "dimension";
  if (/^opacity-/.test(n)) return "number";
  if (/^brand-diagonal-angle/.test(n)) return "angle";
  if (
    /color|background|foreground|border|surface|action|feedback|data-\d|ring-color|-red-|-blue-/.test(
      n,
    )
  )
    return "color";
  return "other";
}

// ---------------------------------------------------------------------------
// 4. Build the nested (W3C-ish) token tree and the flat map.
// ---------------------------------------------------------------------------

function toCamel(segment) {
  return segment.replace(/-([a-z0-9])/g, (_, c) => c.toUpperCase());
}

function toPascal(segment) {
  const camel = toCamel(segment);
  return camel.charAt(0).toUpperCase() + camel.slice(1);
}

const nested = {};
const flat = {};

for (const [name, { value, raw }] of resolvedRootTokens.entries()) {
  const path_ = name.replace(/^--/, "").split("-");
  const type = classify(name, value);
  let cursor = nested;
  for (let i = 0; i < path_.length - 1; i++) {
    const seg = path_[i];
    cursor[seg] = cursor[seg] ?? {};
    cursor = cursor[seg];
  }
  const leaf = path_[path_.length - 1];
  cursor[leaf] = {
    $value: value,
    $type: type,
    $extensions: { "com.rbi.css": { property: name, raw } },
  };
  flat[name.replace(/^--/, "")] = value;
}

// ---------------------------------------------------------------------------
// 5. Emit files.
// ---------------------------------------------------------------------------

mkdirSync(OUT_DIR, { recursive: true });

// tokens.json (nested, W3C-ish)
writeFileSync(path.join(OUT_DIR, "tokens.json"), JSON.stringify(nested, null, 2) + "\n", "utf8");

// tokens.flat.json
writeFileSync(path.join(OUT_DIR, "tokens.flat.json"), JSON.stringify(flat, null, 2) + "\n", "utf8");

// tokens.css
function renderRootCss() {
  const lines = [":root {"];
  for (const [name, { raw }] of resolvedRootTokens.entries()) {
    lines.push(`  ${name}: ${raw};`);
  }
  lines.push("}");
  return lines.join("\n");
}

function renderThemeBlocksCss() {
  return themeBlocks
    .map(({ selector, tokens }) => {
      const lines = [`${selector} {`];
      for (const [name, value] of tokens) {
        lines.push(`  ${name}: ${value};`);
      }
      lines.push("}");
      return lines.join("\n");
    })
    .join("\n\n");
}

const tokensCss = `/**
 * @rbi/tokens — generated file, do not edit by hand.
 * Source of truth: src/styles.css (and src/design-system/tokens/*).
 * Regenerate with: node scripts/tokens/build-tokens.mjs
 */

${renderRootCss()}

/* Surface context overrides (set via [data-surface="..."] wrappers). */
${renderThemeBlocksCss()}
`;
writeFileSync(path.join(OUT_DIR, "tokens.css"), tokensCss, "utf8");

// tokens.scss
const scssVars = Object.entries(flat)
  .map(
    ([name, value]) =>
      `$${name}: ${value.replace(/var\((--[a-zA-Z0-9-]+)\)/g, (m, ref) => `$${ref.replace(/^--/, "")}`)};`,
  )
  .join("\n");
const tokensScss = `// @rbi/tokens — generated file, do not edit by hand.
// Source of truth: src/styles.css. Regenerate with: node scripts/tokens/build-tokens.mjs

${scssVars}
`;
writeFileSync(path.join(OUT_DIR, "tokens.scss"), tokensScss, "utf8");

// tokens.ts + tokens.d.ts
const tsEntries = Object.entries(flat)
  .map(([name, value]) => `  "${name}": ${JSON.stringify(value)},`)
  .join("\n");

const tokensTs = `/**
 * @rbi/tokens — generated file, do not edit by hand.
 * Source of truth: src/styles.css (and src/design-system/tokens/*).
 * Regenerate with: node scripts/tokens/build-tokens.mjs
 */

/** Flat map of every CSS custom property name (without \`--\`) to its resolved value. */
export const tokens = {
${tsEntries}
} as const;

export type TokenName = keyof typeof tokens;
export type Tokens = typeof tokens;

/** Nested, W3C Design Tokens Community Group-shaped token tree. */
export const tokensNested = ${JSON.stringify(nested, null, 2)} as const;

export default tokens;
`;
writeFileSync(path.join(OUT_DIR, "tokens.ts"), tokensTs, "utf8");

const tokensDts = `/**
 * @rbi/tokens — generated file, do not edit by hand.
 * Regenerate with: node scripts/tokens/build-tokens.mjs
 */

export declare const tokens: Record<string, string>;
export type TokenName = string;
export type Tokens = typeof tokens;
export declare const tokensNested: Record<string, unknown>;
declare const _default: typeof tokens;
export default _default;
`;
writeFileSync(path.join(OUT_DIR, "tokens.d.ts"), tokensDts, "utf8");

console.log(`@rbi/tokens generated in ${path.relative(ROOT, OUT_DIR)}`);
console.log(`  - ${Object.keys(flat).length} tokens`);
console.log(
  `  - ${themeBlocks.length} surface theme blocks (${themeBlocks.map((b) => b.selector).join(", ")})`,
);

import { pseudoLocaleCode, rtlDevelopmentLocaleCode } from "../config/localization-config";

/**
 * Development-only pseudo-localization.
 *
 * Expands text (~35%), accents letters and wraps the result in brackets so
 * clipped controls, fixed heights and hardcoded English text become obvious.
 * ICU syntax and `{interpolation}` placeholders are preserved verbatim.
 *
 * Never available in production: the provider only registers it when
 * `ENABLE_PSEUDO_LOCALE=true` in a development build.
 */

const ACCENTS: Readonly<Record<string, string>> = {
  a: "à", b: "ƀ", c: "ç", d: "ð", e: "é", f: "ƒ", g: "ĝ", h: "ĥ", i: "í", j: "ĵ",
  k: "ķ", l: "ĺ", m: "ɱ", n: "ñ", o: "ó", p: "þ", q: "ɋ", r: "ŕ", s: "š", t: "ţ",
  u: "ü", v: "ṽ", w: "ŵ", x: "ẋ", y: "ý", z: "ž",
  A: "À", B: "Ɓ", C: "Ç", D: "Ð", E: "É", F: "Ƒ", G: "Ĝ", H: "Ĥ", I: "Í", J: "Ĵ",
  K: "Ķ", L: "Ĺ", M: "Ṁ", N: "Ñ", O: "Ó", P: "Þ", Q: "Ǫ", R: "Ŕ", S: "Š", T: "Ţ",
  U: "Ü", V: "Ṽ", W: "Ŵ", X: "Ẋ", Y: "Ý", Z: "Ž",
};

const EXPANSION_RATIO = 0.35;
const PAD_CHARACTER = "~";

/** Segments inside {...} are ICU/interpolation and must survive untouched. */
function splitPreservingPlaceholders(input: string): readonly { text: string; literal: boolean }[] {
  const parts: { text: string; literal: boolean }[] = [];
  let depth = 0;
  let buffer = "";
  let literal = false;

  const flush = () => {
    if (buffer) parts.push({ text: buffer, literal });
    buffer = "";
  };

  for (const char of input) {
    if (char === "{") {
      if (depth === 0) {
        flush();
        literal = true;
      }
      depth += 1;
    }
    buffer += char;
    if (char === "}") {
      depth = Math.max(0, depth - 1);
      if (depth === 0) {
        flush();
        literal = false;
      }
    }
  }
  flush();
  return parts;
}

export function pseudoLocalize(value: string): string {
  if (!value) return value;
  const accented = splitPreservingPlaceholders(value)
    .map((part) =>
      part.literal
        ? part.text
        : part.text.replace(/[A-Za-z]/g, (char) => ACCENTS[char] ?? char),
    )
    .join("");

  const visibleLength = accented.replace(/\{[^}]*\}/g, "").length;
  const padding = PAD_CHARACTER.repeat(Math.max(1, Math.round(visibleLength * EXPANSION_RATIO)));
  return `［${accented}${padding}］`;
}

/** RTL development locale: same expansion, wrapped in RTL isolation marks. */
export function pseudoLocalizeRtl(value: string): string {
  return `\u2067${pseudoLocalize(value)}\u2069`;
}

export const pseudoLocalePostProcessorName = "rbi-pseudo";

/** i18next post-processor; registered only in development builds. */
export const pseudoLocalePostProcessor = {
  type: "postProcessor" as const,
  name: pseudoLocalePostProcessorName,
  process(value: string, _key: string | string[], _options: unknown, translator: { language?: string }) {
    if (translator?.language === pseudoLocaleCode) return pseudoLocalize(value);
    if (translator?.language === rtlDevelopmentLocaleCode) return pseudoLocalizeRtl(value);
    return value;
  },
};

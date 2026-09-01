/**
 * WCAG 2.2 contrast maths.
 *
 * Pure, dependency-free helpers so the foundations page can verify a token
 * pair in the browser instead of documenting a ratio a human typed in once.
 */

export interface Rgb {
  readonly r: number;
  readonly g: number;
  readonly b: number;
}

/** Parses `#rgb` / `#rrggbb`. Returns `undefined` for anything else. */
export function parseHex(input: string): Rgb | undefined {
  const hex = input.trim().replace(/^#/, "");
  const expanded =
    hex.length === 3
      ? hex
          .split("")
          .map((character) => character + character)
          .join("")
      : hex;
  if (!/^[0-9a-fA-F]{6}$/.test(expanded)) return undefined;
  return {
    r: Number.parseInt(expanded.slice(0, 2), 16),
    g: Number.parseInt(expanded.slice(2, 4), 16),
    b: Number.parseInt(expanded.slice(4, 6), 16),
  };
}

function channelLuminance(value: number) {
  const channel = value / 255;
  return channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
}

/** Relative luminance per WCAG 2.x. */
export function relativeLuminance({ r, g, b }: Rgb): number {
  return 0.2126 * channelLuminance(r) + 0.7152 * channelLuminance(g) + 0.0722 * channelLuminance(b);
}

/** Contrast ratio between two hex colours, from 1 to 21. */
export function contrastRatio(foreground: string, background: string): number {
  const a = parseHex(foreground);
  const b = parseHex(background);
  if (!a || !b) return 1;
  const light = Math.max(relativeLuminance(a), relativeLuminance(b));
  const dark = Math.min(relativeLuminance(a), relativeLuminance(b));
  return (light + 0.05) / (dark + 0.05);
}

export type ContrastUse = "bodyText" | "largeText" | "uiComponent";

export interface ContrastRequirement {
  readonly id: ContrastUse;
  readonly label: string;
  readonly description: string;
  /** Minimum ratio for the AA success criterion. */
  readonly aa: number;
  /** Minimum ratio for AAA, where the criterion defines one. */
  readonly aaa?: number;
}

/** The three ratio thresholds this system checks, in WCAG 2.2 terms. */
export const contrastRequirements: readonly ContrastRequirement[] = [
  {
    id: "bodyText",
    label: "Body text",
    description: "Text below 24px, or below 19px when bold — SC 1.4.3 / 1.4.6.",
    aa: 4.5,
    aaa: 7,
  },
  {
    id: "largeText",
    label: "Large text",
    description: "Text from 24px, or 19px bold and above — SC 1.4.3 / 1.4.6.",
    aa: 3,
    aaa: 4.5,
  },
  {
    id: "uiComponent",
    label: "UI and graphics",
    description: "Borders, icons, focus rings and control boundaries — SC 1.4.11.",
    aa: 3,
  },
];

export type ContrastVerdict = "fail" | "aa" | "aaa";

export interface ContrastResult {
  readonly ratio: number;
  /** Ratio rounded to two decimals, formatted as `4.62:1`. */
  readonly display: string;
  readonly verdicts: Readonly<Record<ContrastUse, ContrastVerdict>>;
  /** True when every checked use case reaches at least AA. */
  readonly passesAllAa: boolean;
}

function verdictFor(ratio: number, requirement: ContrastRequirement): ContrastVerdict {
  if (requirement.aaa !== undefined && ratio >= requirement.aaa) return "aaa";
  if (ratio >= requirement.aa) return "aa";
  return "fail";
}

/** Evaluates a foreground/background pair against every requirement. */
export function evaluateContrast(foreground: string, background: string): ContrastResult {
  const ratio = contrastRatio(foreground, background);
  const verdicts = Object.fromEntries(
    contrastRequirements.map((requirement) => [requirement.id, verdictFor(ratio, requirement)]),
  ) as Record<ContrastUse, ContrastVerdict>;

  return {
    ratio,
    display: `${(Math.floor(ratio * 100) / 100).toFixed(2)}:1`,
    verdicts,
    passesAllAa: Object.values(verdicts).every((verdict) => verdict !== "fail"),
  };
}

/** Whichever of the two candidates reads best on the given background. */
export function bestForeground(background: string, candidates: readonly string[]): string {
  return [...candidates].sort((a, b) => contrastRatio(b, background) - contrastRatio(a, background))[0] ?? candidates[0]!;
}

import { colorTokenGroups } from "@/design-system/tokens";

/**
 * The selectable palette behind the contrast checker.
 *
 * Derived from the token catalog rather than re-typed, so a palette change in
 * `colors.ts` immediately shows up in the checker. Only primitive tokens carry
 * a literal hex value; semantic tokens alias them and are therefore excluded.
 */

export interface PaletteOption {
  /** CSS custom property, e.g. `--rbi-yellow-400`. */
  readonly token: string;
  readonly label: string;
  readonly hex: string;
  /** Title of the token group the colour belongs to. */
  readonly group: string;
}

const HEX = /^#[0-9a-fA-F]{6}$/;

export const paletteOptions: readonly PaletteOption[] = colorTokenGroups
  .filter((group) => group.layer === "primitive")
  .flatMap((group) =>
    group.tokens
      .filter((entry) => HEX.test(entry.value))
      .map((entry) => ({
        token: entry.token,
        label: entry.label,
        hex: entry.value.toUpperCase(),
        group: group.title,
      })),
  );

export const paletteByToken: ReadonlyMap<string, PaletteOption> = new Map(
  paletteOptions.map((option) => [option.token, option] as const),
);

/** Options grouped for a `<Select>` with option groups. */
export const paletteGroups: readonly { readonly group: string; readonly options: readonly PaletteOption[] }[] =
  Array.from(
    paletteOptions.reduce((accumulator, option) => {
      const bucket = accumulator.get(option.group) ?? [];
      bucket.push(option);
      accumulator.set(option.group, bucket);
      return accumulator;
    }, new Map<string, PaletteOption[]>()),
    ([group, options]) => ({ group, options }),
  );

/** Sensible defaults: the brand pair every RBI screen starts from. */
export const defaultContrastPair = {
  foreground: "--rbi-off-black",
  background: "--rbi-yellow-400",
} as const;

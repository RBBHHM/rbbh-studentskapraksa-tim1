import type { BentoRowSpan, BentoSpan, BentoTone } from "@/components/ui/bento-grid";

/**
 * Bento blueprints.
 *
 * The builder on the Patterns page works on this data shape and serialises it
 * back to JSX, so the code a consumer copies is generated from the same object
 * that rendered the preview — there is no second, hand-written snippet that can
 * drift from what they see.
 */

export interface BentoTile {
  readonly id: string;
  readonly eyebrow?: string | undefined;
  readonly title: string;
  readonly body?: string | undefined;
  readonly span: BentoSpan;
  readonly rowSpan: BentoRowSpan;
  readonly tone: BentoTone;
  readonly accent: boolean;
}

export interface BentoPreset {
  readonly id: string;
  /** Localization key segment under `components:bentoBuilder.presets`. */
  readonly messageKey: string;
  readonly label: string;
  readonly description: string;
  readonly tiles: readonly BentoTile[];
}

/** The span values the builder cycles through, in order. */
export const bentoSpanOptions: readonly BentoSpan[] = [3, 4, 6, 8, 9, 12];
export const bentoRowSpanOptions: readonly BentoRowSpan[] = [1, 2];
export const bentoToneOptions: readonly BentoTone[] = ["default", "subtle", "brand", "corporate", "inverse"];

export const bentoPresets: readonly BentoPreset[] = [
  {
    id: "hero-split",
    messageKey: "heroSplit",
    label: "Hero split",
    description: "One wide lead tile with a tall companion, then a three-up row. The default marketing layout.",
    tiles: [
      {
        id: "lead",
        eyebrow: "Lead",
        title: "Make it happen",
        body: "The widest tile carries the promise. Keep it to one sentence — the grid does the rest of the work.",
        span: 8,
        rowSpan: 2,
        tone: "brand",
        accent: false,
      },
      {
        id: "aside",
        eyebrow: "Proof",
        title: "17 markets",
        body: "A single figure beside the lead gives the claim weight without a second paragraph.",
        span: 4,
        rowSpan: 2,
        tone: "inverse",
        accent: false,
      },
      { id: "one", title: "Foundations", body: "Tokens, type and colour.", span: 4, rowSpan: 1, tone: "default", accent: true },
      { id: "two", title: "Components", body: "Accessible, documented, live.", span: 4, rowSpan: 1, tone: "default", accent: true },
      { id: "three", title: "Patterns", body: "Approved compositions.", span: 4, rowSpan: 1, tone: "default", accent: true },
    ],
  },
  {
    id: "feature-mosaic",
    messageKey: "featureMosaic",
    label: "Feature mosaic",
    description: "Alternating wide and narrow tiles. Good for six features where two deserve more room.",
    tiles: [
      { id: "a", eyebrow: "01", title: "Token layer", body: "Primitives feed semantics; components consume semantics only.", span: 6, rowSpan: 1, tone: "subtle", accent: true },
      { id: "b", eyebrow: "02", title: "Surface contexts", body: "Light, subtle, brand, corporate and inverse from one attribute.", span: 3, rowSpan: 1, tone: "default", accent: false },
      { id: "c", eyebrow: "03", title: "Amalia", body: "Self-hosted, six weights.", span: 3, rowSpan: 1, tone: "default", accent: false },
      { id: "d", eyebrow: "04", title: "Accessibility", body: "AA pairs and documented keyboard behaviour.", span: 4, rowSpan: 1, tone: "corporate", accent: false },
      { id: "e", eyebrow: "05", title: "Typed API", body: "One contract shared by the client and the service.", span: 8, rowSpan: 1, tone: "default", accent: true },
    ],
  },
  {
    id: "dashboard",
    messageKey: "dashboard",
    label: "Dashboard",
    description: "A metric row above a wide chart tile and a narrow activity rail. The back-office layout.",
    tiles: [
      { id: "m1", eyebrow: "Exposure", title: "EUR 4.2bn", body: "Across all segments.", span: 3, rowSpan: 1, tone: "default", accent: true },
      { id: "m2", eyebrow: "Accounts", title: "1,284", body: "912 active.", span: 3, rowSpan: 1, tone: "default", accent: true },
      { id: "m3", eyebrow: "In review", title: "37", body: "Awaiting a second approver.", span: 3, rowSpan: 1, tone: "default", accent: true },
      { id: "m4", eyebrow: "Blocked", title: "6", body: "Escalated to compliance.", span: 3, rowSpan: 1, tone: "default", accent: true },
      { id: "chart", eyebrow: "Volume", title: "Settlement volume", body: "The widest tile hosts the chart; the rail beside it lists what changed.", span: 8, rowSpan: 2, tone: "subtle", accent: false },
      { id: "rail", eyebrow: "Activity", title: "Latest changes", body: "A tall narrow tile reads as a feed rather than a card.", span: 4, rowSpan: 2, tone: "default", accent: false },
    ],
  },
  {
    id: "editorial",
    messageKey: "editorial",
    label: "Editorial",
    description: "Full-width statement, then two equal columns. The quietest option — best for long copy.",
    tiles: [
      { id: "statement", eyebrow: "Statement", title: "One design language, seventeen markets", body: "A full-width tile is a section heading that happens to be a card.", span: 12, rowSpan: 1, tone: "brand", accent: false },
      { id: "left", title: "Why it exists", body: "So two teams building two products arrive at the same rhythm and the same use of yellow.", span: 6, rowSpan: 1, tone: "default", accent: true },
      { id: "right", title: "How it is governed", body: "New values enter the primitive layer through brand governance, never through a component.", span: 6, rowSpan: 1, tone: "default", accent: true },
    ],
  },
];

const INDENT = "  ";

function attribute(name: string, value: string | number | boolean) {
  if (typeof value === "boolean") return value ? ` ${name}` : "";
  if (typeof value === "number") return ` ${name}={${value}}`;
  return ` ${name}="${value}"`;
}

/** Serialises a tile set to the JSX a consumer can paste into a route. */
export function serializeBento(
  tiles: readonly BentoTile[],
  options: { readonly gap: "tight" | "default" | "loose"; readonly density: "uniform" | "tall" },
): string {
  const body = tiles
    .map((tile) => {
      const open = [
        `${INDENT}<BentoCard`,
        attribute("span", tile.span),
        tile.rowSpan === 1 ? "" : attribute("rowSpan", tile.rowSpan),
        tile.tone === "default" ? "" : attribute("tone", tile.tone),
        attribute("accent", tile.accent),
        ">",
      ].join("");

      const bodyProps = [
        tile.eyebrow ? `${INDENT.repeat(3)}eyebrow="${tile.eyebrow}"` : "",
        `${INDENT.repeat(3)}title="${tile.title}"`,
        tile.body ? `${INDENT.repeat(3)}body="${tile.body}"` : "",
      ]
        .filter(Boolean)
        .join("\n");

      return [open, `${INDENT.repeat(2)}<BentoCardBody`, bodyProps, `${INDENT.repeat(2)}/>`, `${INDENT}</BentoCard>`].join(
        "\n",
      );
    })
    .join("\n");

  return [
    `import { BentoCard, BentoCardBody, BentoGrid } from "@/components/ui/bento-grid";`,
    "",
    `<BentoGrid gap="${options.gap}" density="${options.density}">`,
    body,
    "</BentoGrid>",
  ].join("\n");
}

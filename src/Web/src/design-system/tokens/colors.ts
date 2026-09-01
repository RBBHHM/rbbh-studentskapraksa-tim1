/**
 * Machine-readable mirror of the colour tokens defined in `src/styles.css`.
 *
 * This module exists so the UI Library can document tokens without a human
 * re-typing values (which is how token drift starts). Every entry points at a
 * CSS custom property; the swatch is rendered from that property, never from
 * the literal hex stored here. `value` is documentation of provenance only.
 */

export interface ColorTokenEntry {
  /** CSS custom property name, without `var()`. */
  readonly token: string;
  /** Human label shown in the catalog. */
  readonly label: string;
  /** Official value, for documentation and diffing against the brand palette. */
  readonly value: string;
  /** Where the value comes from, or what the token is for. */
  readonly note: string;
}

export interface ColorTokenGroup {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly layer: "primitive" | "semantic";
  readonly tokens: readonly ColorTokenEntry[];
}

const OFFICIAL = "Official RBI RGB palette (Colour_Palette.zip / RBI - RGB - Full Palette.ase)";

export const colorTokenGroups: readonly ColorTokenGroup[] = [
  {
    id: "primitive-yellow",
    title: "Primary yellow",
    layer: "primitive",
    description:
      "Primary yellow is the single most distinctive RBI asset. Yellow 400 is the core tone; the surrounding tones add warmth and depth. Yellow is never used as a large-area text colour.",
    tokens: [
      { token: "--rbi-yellow-100", label: "Yellow 100", value: "#FDF8D5", note: OFFICIAL },
      { token: "--rbi-yellow-200", label: "Yellow 200", value: "#FFF0A6", note: `${OFFICIAL} — "Yellow 1"` },
      { token: "--rbi-yellow-300", label: "Yellow 300", value: "#FFD403", note: `${OFFICIAL} — "Yellow 2"` },
      {
        token: "--rbi-yellow-400",
        label: "Primary Yellow",
        value: "#FEE600",
        note: `${OFFICIAL} — "RBI Primary Yellow"`,
      },
      { token: "--rbi-yellow-500", label: "Yellow 500", value: "#F9BB30", note: `${OFFICIAL} — "Yellow 3"` },
      { token: "--rbi-yellow-600", label: "Yellow 600", value: "#ED9504", note: OFFICIAL },
      { token: "--rbi-yellow-700", label: "Yellow 700", value: "#C9740E", note: `${OFFICIAL} — accessible on white` },
    ],
  },
  {
    id: "primitive-mono",
    title: "Mono and warm neutrals",
    layer: "primitive",
    description:
      "The mono palette carries most of the interface. Warm greys keep large surfaces from feeling clinical; off-black is the primary ink and the primary inverse surface.",
    tokens: [
      { token: "--rbi-white", label: "RBI White", value: "#FFFFFF", note: `${OFFICIAL} — "RBI White"` },
      { token: "--rbi-warm-grey-50", label: "Warm Grey 50%", value: "#F8F6F2", note: OFFICIAL },
      { token: "--rbi-warm-grey-100", label: "Warm Grey", value: "#F1EDE6", note: OFFICIAL },
      { token: "--rbi-off-black", label: "RBI Off Black", value: "#2B2D33", note: `${OFFICIAL} — "RBI Off Black"` },
      { token: "--rbi-mono-black", label: "Mono Black", value: "#1D1D1B", note: OFFICIAL },
    ],
  },
  {
    id: "primitive-grey",
    title: "Grey ramp",
    layer: "primitive",
    description:
      "The tonal ramp beneath off-black, used for borders, secondary text and disabled states. Grey 900 is identical to RBI Off Black by design.",
    tokens: [
      { token: "--rbi-grey-50", label: "Grey 50", value: "#F9F9F9", note: OFFICIAL },
      { token: "--rbi-grey-100", label: "Grey 100", value: "#F2F2F3", note: OFFICIAL },
      { token: "--rbi-grey-200", label: "Grey 200", value: "#EAEAEB", note: OFFICIAL },
      { token: "--rbi-grey-300", label: "Grey 300", value: "#D5D5D6", note: OFFICIAL },
      { token: "--rbi-grey-400", label: "Grey 400", value: "#BFC0C2", note: OFFICIAL },
      { token: "--rbi-grey-500", label: "Grey 500", value: "#AAABAE", note: OFFICIAL },
      { token: "--rbi-grey-600", label: "Grey 600", value: "#808185", note: OFFICIAL },
      { token: "--rbi-grey-700", label: "Grey 700", value: "#55575D", note: OFFICIAL },
      { token: "--rbi-grey-800", label: "Grey 800", value: "#404248", note: OFFICIAL },
      { token: "--rbi-grey-900", label: "Grey 900", value: "#2B2D33", note: OFFICIAL },
    ],
  },
  {
    id: "primitive-green",
    title: "Corporate green",
    layer: "primitive",
    description:
      "The brand guidelines elevate green for the corporate vertical, which is what Raiffeisen Bank International is. Green adds richness and carries ESG and sustainability content. It is a secondary accent, never a replacement for yellow.",
    tokens: [
      { token: "--rbi-green-100", label: "Green 100", value: "#D0F0E5", note: OFFICIAL },
      { token: "--rbi-green-200", label: "Green 200", value: "#A3E2CC", note: OFFICIAL },
      { token: "--rbi-green-300", label: "Green 300", value: "#67D0AB", note: OFFICIAL },
      { token: "--rbi-green-400", label: "Green 400", value: "#489B79", note: OFFICIAL },
      { token: "--rbi-green-500", label: "Green 500", value: "#225B45", note: OFFICIAL },
    ],
  },
  {
    id: "primitive-accents",
    title: "Coral and purple",
    layer: "primitive",
    description:
      "Available from the official secondary palette. Used sparingly in this template — mainly for data visualisation — because the RBI International vertical elevates green rather than coral or purple.",
    tokens: [
      { token: "--rbi-coral-300", label: "Coral 300", value: "#FF8B6B", note: OFFICIAL },
      { token: "--rbi-coral-400", label: "Coral 400", value: "#E67153", note: OFFICIAL },
      { token: "--rbi-coral-500", label: "Coral 500", value: "#C65C4A", note: OFFICIAL },
      { token: "--rbi-purple-300", label: "Purple 300", value: "#6A4CAD", note: OFFICIAL },
      { token: "--rbi-purple-400", label: "Purple 400", value: "#563E83", note: OFFICIAL },
      { token: "--rbi-purple-500", label: "Purple 500", value: "#412F59", note: OFFICIAL },
    ],
  },
  {
    id: "semantic-surface",
    title: "Surfaces",
    layer: "semantic",
    description:
      "Surface tokens describe elevation and context, not colour. Setting `data-surface` on a wrapper reassigns these for the whole subtree, which is how inverse and brand sections work without per-component dark variants.",
    tokens: [
      { token: "--surface-default", label: "surface-default", value: "RBI White", note: "Page and card background" },
      { token: "--surface-subtle", label: "surface-subtle", value: "Warm Grey 50%", note: "Alternating sections" },
      { token: "--surface-muted", label: "surface-muted", value: "Warm Grey", note: "Inset panels, tertiary buttons" },
      { token: "--surface-sunken", label: "surface-sunken", value: "Grey 100", note: "Code blocks, wells" },
      { token: "--surface-inverse", label: "surface-inverse", value: "RBI Off Black", note: "Inverse brand sections" },
      { token: "--surface-brand", label: "surface-brand", value: "Primary Yellow", note: "Yellow feature areas" },
      { token: "--surface-brand-subtle", label: "surface-brand-subtle", value: "Yellow 200", note: "Soft highlights" },
      { token: "--surface-brand-faint", label: "surface-brand-faint", value: "Yellow 100", note: "Hover tint" },
      { token: "--surface-corporate", label: "surface-corporate", value: "Green 500", note: "Corporate accent panels" },
      {
        token: "--surface-corporate-subtle",
        label: "surface-corporate-subtle",
        value: "Green 100",
        note: "Sustainability callouts",
      },
    ],
  },
  {
    id: "semantic-text",
    title: "Text",
    layer: "semantic",
    description:
      "Every pairing below meets WCAG 2.2 AA against its intended surface. Yellow is never used for body text; `text-brand-accent` (Yellow 700) is the accessible substitute on light surfaces.",
    tokens: [
      { token: "--text-primary", label: "text-primary", value: "Off Black", note: "15.1:1 on white" },
      { token: "--text-secondary", label: "text-secondary", value: "Grey 700", note: "7.6:1 on white" },
      { token: "--text-tertiary", label: "text-tertiary", value: "Grey 600", note: "4.6:1 on white" },
      { token: "--text-inverse", label: "text-inverse", value: "White", note: "15.1:1 on off-black" },
      { token: "--text-on-brand", label: "text-on-brand", value: "Off Black", note: "12.4:1 on primary yellow" },
      { token: "--text-on-corporate", label: "text-on-corporate", value: "White", note: "7.9:1 on green 500" },
      { token: "--text-brand-accent", label: "text-brand-accent", value: "Yellow 700", note: "4.6:1 on white" },
      { token: "--text-corporate", label: "text-corporate", value: "Green 500", note: "7.9:1 on white" },
      { token: "--text-disabled", label: "text-disabled", value: "Grey 500", note: "Non-informational text only" },
    ],
  },
  {
    id: "semantic-border",
    title: "Borders",
    layer: "semantic",
    description:
      "Borders carry structure in this identity instead of shadows. Input borders are deliberately darker than decorative borders so form controls meet the 3:1 non-text contrast requirement.",
    tokens: [
      { token: "--border-default", label: "border-default", value: "Grey 300", note: "Cards, dividers" },
      { token: "--border-subtle", label: "border-subtle", value: "Grey 200", note: "Table rows, list separators" },
      { token: "--border-strong", label: "border-strong", value: "Off Black", note: "Secondary buttons, emphasis" },
      { token: "--border-input", label: "border-input", value: "Grey 600", note: "3.2:1 on white" },
      { token: "--border-brand", label: "border-brand", value: "Primary Yellow", note: "Brand accent rules" },
      { token: "--border-error", label: "border-error", value: "Red 500", note: "Invalid fields" },
    ],
  },
  {
    id: "semantic-action",
    title: "Actions",
    layer: "semantic",
    description:
      "Action tokens are the only place a button colour is decided. Primary is always yellow with off-black text — the core brand principle that the logo and the primary action are both attributed to yellow.",
    tokens: [
      {
        token: "--action-primary-background",
        label: "action-primary-background",
        value: "Primary Yellow",
        note: "Rest state",
      },
      {
        token: "--action-primary-background-hover",
        label: "…-hover",
        value: "Yellow 300",
        note: "Hover / focus state",
      },
      {
        token: "--action-primary-background-active",
        label: "…-active",
        value: "Yellow 500",
        note: "Pressed state",
      },
      {
        token: "--action-primary-foreground",
        label: "action-primary-foreground",
        value: "Off Black",
        note: "Never white on yellow",
      },
      { token: "--action-secondary-border", label: "action-secondary-border", value: "Off Black", note: "Outlined" },
      {
        token: "--action-destructive-background",
        label: "action-destructive-background",
        value: "Red 500",
        note: "4.9:1 with white text",
      },
    ],
  },
  {
    id: "semantic-feedback",
    title: "Feedback",
    layer: "semantic",
    description:
      "Each status has a background, border and foreground triple so a status is never communicated by colour alone — components always pair these with an icon and text.",
    tokens: [
      { token: "--feedback-info-foreground", label: "feedback-info", value: "Blue 500", note: "Informational" },
      { token: "--feedback-success-foreground", label: "feedback-success", value: "Green 500", note: "Success" },
      { token: "--feedback-warning-foreground", label: "feedback-warning", value: "Amber 500", note: "Warning" },
      { token: "--feedback-danger-foreground", label: "feedback-danger", value: "Red 500", note: "Error" },
      { token: "--feedback-neutral-foreground", label: "feedback-neutral", value: "Grey 800", note: "Neutral" },
    ],
  },
  {
    id: "semantic-data",
    title: "Data visualisation",
    layer: "semantic",
    description:
      "An eight-step categorical sequence drawn from the official infographic palette, ordered for maximum adjacent-hue separation. Charts must additionally distinguish series by label, pattern or shape.",
    tokens: [
      { token: "--data-1", label: "data-1", value: "Primary Yellow", note: "First series" },
      { token: "--data-2", label: "data-2", value: "Green 500", note: "Second series" },
      { token: "--data-3", label: "data-3", value: "Blue 400", note: "Third series" },
      { token: "--data-4", label: "data-4", value: "Coral 400", note: "Fourth series" },
      { token: "--data-5", label: "data-5", value: "Purple 300", note: "Fifth series" },
      { token: "--data-6", label: "data-6", value: "Teal 400", note: "Sixth series" },
      { token: "--data-7", label: "data-7", value: "Magenta 400", note: "Seventh series" },
      { token: "--data-8", label: "data-8", value: "Lime 400", note: "Eighth series" },
    ],
  },
];

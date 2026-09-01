/**
 * Machine-readable mirror of the non-colour tokens in `src/styles.css`.
 * Used by the UI Library foundations sections so documentation cannot drift
 * from the implementation.
 */

export interface ScaleEntry {
  readonly token: string;
  readonly label: string;
  readonly value: string;
  readonly usage: string;
}

export const fontWeightScale: readonly ScaleEntry[] = [
  { token: "--font-weight-thin", label: "Thin", value: "100", usage: "Reserved for oversized editorial display only" },
  { token: "--font-weight-light", label: "Light", value: "300", usage: "Large intro paragraphs at 22px and above" },
  { token: "--font-weight-regular", label: "Regular", value: "400", usage: "Body copy, form values, table cells" },
  { token: "--font-weight-medium", label: "Medium", value: "500", usage: "Labels, buttons, navigation, table headers" },
  { token: "--font-weight-bold", label: "Bold", value: "700", usage: "Headings, emphasis, statistic values" },
  { token: "--font-weight-black", label: "Black", value: "900", usage: "Display headlines and hero statements" },
];

export const fontSizeScale: readonly ScaleEntry[] = [
  { token: "--font-size-2xs", label: "2xs", value: "11px", usage: "Legal footnotes only" },
  { token: "--font-size-xs", label: "xs", value: "12px", usage: "Eyebrows, badges, table captions" },
  { token: "--font-size-sm", label: "sm", value: "14px", usage: "Hints, secondary metadata, dense tables" },
  { token: "--font-size-md", label: "md", value: "16px", usage: "Body default — never smaller for prose" },
  { token: "--font-size-lg", label: "lg", value: "18px", usage: "Lead paragraphs, card titles" },
  { token: "--font-size-xl", label: "xl", value: "22px", usage: "h4 / section subheadings" },
  { token: "--font-size-2xl", label: "2xl", value: "28px", usage: "h3" },
  { token: "--font-size-3xl", label: "3xl", value: "36px", usage: "h2 / page titles" },
  { token: "--font-size-4xl", label: "4xl", value: "48px", usage: "h1" },
  { token: "--font-size-5xl", label: "5xl", value: "64px", usage: "Hero headline (desktop)" },
  { token: "--font-size-6xl", label: "6xl", value: "84px", usage: "Editorial statement, large viewports only" },
];

export const lineHeightScale: readonly ScaleEntry[] = [
  { token: "--line-height-tight", label: "tight", value: "1.05", usage: "Display type 48px and above" },
  { token: "--line-height-snug", label: "snug", value: "1.2", usage: "Headings" },
  { token: "--line-height-normal", label: "normal", value: "1.4", usage: "UI labels, table cells" },
  { token: "--line-height-relaxed", label: "relaxed", value: "1.6", usage: "Body prose" },
];

export const letterSpacingScale: readonly ScaleEntry[] = [
  { token: "--letter-spacing-tighter", label: "tighter", value: "-0.03em", usage: "Display headlines" },
  { token: "--letter-spacing-tight", label: "tight", value: "-0.015em", usage: "Headings" },
  { token: "--letter-spacing-normal", label: "normal", value: "0", usage: "Body and UI" },
  { token: "--letter-spacing-wide", label: "wide", value: "0.04em", usage: "Small caps buttons" },
  { token: "--letter-spacing-wider", label: "wider", value: "0.12em", usage: "Eyebrow labels" },
];

export const spacingScale: readonly ScaleEntry[] = [
  { token: "--space-1", label: "1", value: "4px", usage: "Icon-to-label gap" },
  { token: "--space-2", label: "2", value: "8px", usage: "Inline control gaps" },
  { token: "--space-3", label: "3", value: "12px", usage: "Compact padding" },
  { token: "--space-4", label: "4", value: "16px", usage: "Default padding and gutters" },
  { token: "--space-5", label: "5", value: "24px", usage: "Card padding, grid gap" },
  { token: "--space-6", label: "6", value: "32px", usage: "Group separation" },
  { token: "--space-7", label: "7", value: "40px", usage: "Sub-section spacing" },
  { token: "--space-8", label: "8", value: "48px", usage: "Section padding (mobile)" },
  { token: "--space-9", label: "9", value: "64px", usage: "Section padding" },
  { token: "--space-10", label: "10", value: "80px", usage: "Section padding (large)" },
  { token: "--space-11", label: "11", value: "104px", usage: "Hero padding" },
  { token: "--space-12", label: "12", value: "128px", usage: "Editorial breathing room" },
];

export const sizeScale: readonly ScaleEntry[] = [
  { token: "--size-control-sm", label: "control-sm", value: "32px", usage: "Dense toolbars — pair with a larger hit area" },
  { token: "--size-control-md", label: "control-md", value: "44px", usage: "Default control height, meets WCAG 2.2 target size" },
  { token: "--size-control-lg", label: "control-lg", value: "52px", usage: "Primary calls to action, mobile forms" },
  { token: "--size-icon-sm", label: "icon-sm", value: "16px", usage: "Inline with 14px text" },
  { token: "--size-icon-md", label: "icon-md", value: "20px", usage: "Default, inline with 16px text" },
  { token: "--size-icon-lg", label: "icon-lg", value: "24px", usage: "Buttons, navigation" },
  { token: "--size-icon-xl", label: "icon-xl", value: "32px", usage: "Feature cards, empty states" },
];

export const borderWidthScale: readonly ScaleEntry[] = [
  { token: "--border-width-hairline", label: "hairline", value: "1px", usage: "Table and list separators" },
  { token: "--border-width-medium", label: "medium", value: "2px", usage: "Interactive outlines, focus ring" },
  { token: "--border-width-thick", label: "thick", value: "3px", usage: "Brand rule, active tab indicator" },
  { token: "--border-width-accent", label: "accent", value: "6px", usage: "Callout leading edge, quote bar" },
];

export const radiusScale: readonly ScaleEntry[] = [
  { token: "--radii-none", label: "none", value: "0px", usage: "Brand panels, diagonal shapes, hero blocks" },
  { token: "--radii-xs", label: "xs", value: "2px", usage: "Tags, focus ring rounding" },
  { token: "--radii-sm", label: "sm", value: "4px", usage: "Default — buttons, inputs, cards" },
  { token: "--radii-md", label: "md", value: "6px", usage: "Popovers, dropdowns" },
  { token: "--radii-lg", label: "lg", value: "10px", usage: "Dialogs, drawers" },
  { token: "--radii-pill", label: "pill", value: "999px", usage: "Status badges and avatars only" },
];

export const elevationScale: readonly ScaleEntry[] = [
  { token: "--elevation-xs", label: "xs", value: "1px / 8% off-black", usage: "Resting cards that need edge definition" },
  { token: "--elevation-sm", label: "sm", value: "3px / 10% off-black", usage: "Hovered cards, sticky headers" },
  { token: "--elevation-md", label: "md", value: "12px / 12% off-black", usage: "Dropdowns, popovers, hover cards" },
  { token: "--elevation-lg", label: "lg", value: "28px / 16% off-black", usage: "Drawers and sheets" },
  { token: "--elevation-overlay", label: "overlay", value: "56px / 28% off-black", usage: "Modal dialogs" },
];

export const zIndexScale: readonly ScaleEntry[] = [
  { token: "--z-base", label: "base", value: "0", usage: "Document flow" },
  { token: "--z-raised", label: "raised", value: "10", usage: "Cards lifted on hover" },
  { token: "--z-sticky", label: "sticky", value: "20", usage: "Sticky catalog navigation" },
  { token: "--z-header", label: "header", value: "30", usage: "Application header" },
  { token: "--z-drawer", label: "drawer", value: "40", usage: "Drawer and sheet surfaces" },
  { token: "--z-overlay", label: "overlay", value: "50", usage: "Modal scrim" },
  { token: "--z-modal", label: "modal", value: "60", usage: "Dialog surface" },
  { token: "--z-popover", label: "popover", value: "70", usage: "Popover, tooltip, menu" },
  { token: "--z-toast", label: "toast", value: "80", usage: "Toast region" },
  { token: "--z-skip-link", label: "skip-link", value: "90", usage: "Skip-to-content link on focus" },
];

export const breakpointScale: readonly ScaleEntry[] = [
  { token: "--breakpoint-sm", label: "sm", value: "640px", usage: "Large phone — two-column cards appear" },
  { token: "--breakpoint-md", label: "md", value: "768px", usage: "Tablet — side-by-side form fields" },
  { token: "--breakpoint-lg", label: "lg", value: "1024px", usage: "Desktop — sticky catalog nav, full tables" },
  { token: "--breakpoint-xl", label: "xl", value: "1280px", usage: "Wide desktop — editorial hero scale" },
  { token: "--breakpoint-2xl", label: "2xl", value: "1536px", usage: "Very wide — content stops growing" },
];

export const contentWidthScale: readonly ScaleEntry[] = [
  { token: "--content-width-prose", label: "prose", value: "68ch", usage: "Long-form documentation copy" },
  { token: "--content-width-narrow", label: "narrow", value: "704px", usage: "Single-column forms, confirmation screens" },
  { token: "--content-width-default", label: "default", value: "1216px", usage: "Standard page container" },
  { token: "--content-width-wide", label: "wide", value: "1440px", usage: "Dense tables and catalogs" },
];

export const motionDurationScale: readonly ScaleEntry[] = [
  { token: "--duration-instant", label: "instant", value: "80ms", usage: "Colour and border feedback" },
  { token: "--duration-fast", label: "fast", value: "140ms", usage: "Hover, focus, small state changes" },
  { token: "--duration-normal", label: "normal", value: "220ms", usage: "Popovers, accordions, tabs" },
  { token: "--duration-slow", label: "slow", value: "360ms", usage: "Drawers and dialogs entering" },
];

export const motionEasingScale: readonly ScaleEntry[] = [
  { token: "--easing-standard", label: "standard", value: "cubic-bezier(0.2, 0, 0, 1)", usage: "Default for all state changes" },
  { token: "--easing-decelerate", label: "decelerate", value: "cubic-bezier(0, 0, 0.2, 1)", usage: "Elements entering the screen" },
  { token: "--easing-accelerate", label: "accelerate", value: "cubic-bezier(0.4, 0, 1, 1)", usage: "Elements leaving the screen" },
  { token: "--easing-emphasised", label: "emphasised", value: "cubic-bezier(0.32, 0.72, 0, 1)", usage: "Drawers and large surfaces" },
];

export const focusStateScale: readonly ScaleEntry[] = [
  { token: "--focus-ring-color", label: "ring colour", value: "Off Black", usage: "Light and brand surfaces" },
  { token: "--focus-ring-color-inverse", label: "ring colour (inverse)", value: "Primary Yellow", usage: "Off-black surfaces" },
  { token: "--focus-ring-width", label: "ring width", value: "2px", usage: "Always 2px — never thinner" },
  { token: "--focus-ring-offset", label: "ring offset", value: "2px", usage: "Keeps the ring clear of the control edge" },
  { token: "--opacity-disabled", label: "disabled opacity", value: "0.45", usage: "Applied with aria-disabled, never alone" },
];

export const gridScale: readonly ScaleEntry[] = [
  { token: "--grid-columns", label: "columns", value: "12", usage: "Base layout grid" },
  { token: "--grid-gap", label: "gap", value: "24px", usage: "Default column and row gap" },
  { token: "--grid-gap-tight", label: "gap-tight", value: "16px", usage: "Dense card grids" },
  { token: "--content-gutter", label: "gutter", value: "16px", usage: "Page edge padding below lg" },
  { token: "--content-gutter-lg", label: "gutter-lg", value: "32px", usage: "Page edge padding at lg and above" },
];

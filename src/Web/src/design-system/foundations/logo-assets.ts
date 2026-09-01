const assetUrl = (filename: string) => `/assets/logos/${filename}`;

/**
 * The official Raiffeisen Bank International "Make it happen" logo lock-ups.
 *
 * These are the customer-supplied RGB PNG assets from the official
 * `Make_it_Happen_Logos.zip` archive, cropped to their artwork bounding box and
 * resampled to a web-appropriate resolution. Nothing about the artwork itself
 * has been altered: no recolouring, redrawing, cropping into the mark,
 * distortion, or CSS/text recreation.
 *
 * CMYK and Pantone variants from the archive are intentionally not shipped —
 * they are print-only colour spaces.
 */

export type RbiLogoVariant =
  /* "Make it happen" endorsed lock-ups — the corporate signature. */
  | "colour"
  | "colourInverse"
  | "mono"
  | "monoInverse"
  | "yellowInverse"
  /* Plain "Raiffeisen Bank" lock-ups — used for product chrome such as the
     application header, where the endorsement line is too much at small sizes. */
  | "bankMono"
  | "bankYellowInverse"
  | "bankMark";

interface LogoAsset {
  readonly url: string;
  readonly width: number;
  readonly height: number;
  /** Which background this variant is approved for. */
  readonly approvedOn: string;
  readonly officialName: string;
}

/**
 * Intrinsic aspect ratios of the shipped artwork. Declaring width and height on
 * every `<img>` reserves layout space and prevents shift while the asset loads.
 */
const SQUARED_LOCKUP = { width: 1400, height: 415 } as const;
const PRIMARY_LOCKUP = { width: 1400, height: 410 } as const;
const BANK_LOCKUP = { width: 1200, height: 302 } as const;
const BANK_MARK = { width: 385, height: 385 } as const;

export const rbiLogoAssets: Readonly<Record<RbiLogoVariant, LogoAsset>> = {
  colour: {
    url: assetUrl("rbi-logo-col-pos.png"),
    ...SQUARED_LOCKUP,
    approvedOn: "White and warm grey surfaces",
    officialName: "RBI-Logo-Bank international Make it happen-St-Squared-Col-Pos-RGB",
  },
  colourInverse: {
    url: assetUrl("rbi-logo-col-neg.png"),
    ...SQUARED_LOCKUP,
    approvedOn: "Off-black and photographic surfaces",
    officialName: "RBI-Logo-Bank international Make it happen-St-Squared-Col-Neg-RGB",
  },
  mono: {
    url: assetUrl("rbi-logo-mono-pos.png"),
    ...PRIMARY_LOCKUP,
    approvedOn: "Single-colour reproduction on light surfaces",
    officialName: "RBI-Logo-Bank international Make it happen-St-Mono-Pos-RGB",
  },
  monoInverse: {
    url: assetUrl("rbi-logo-mono-neg.png"),
    ...PRIMARY_LOCKUP,
    approvedOn: "Single-colour reproduction on off-black or yellow surfaces",
    officialName: "RBI-Logo-Bank international Make it happen-St-Mono-Neg-RGB",
  },
  yellowInverse: {
    url: assetUrl("rbi-logo-yellow-neg.png"),
    ...PRIMARY_LOCKUP,
    approvedOn: "Off-black surfaces only",
    officialName: "RBI-Logo-Bank international Make it happen-St-Yell-Neg-RGB",
  },
  bankMono: {
    url: assetUrl("rbi-bank-mono-pos.png"),
    ...BANK_LOCKUP,
    approvedOn: "White and warm grey surfaces",
    officialName: "RBI-Logo-Bank-St-Mono-Pos-RGB",
  },
  bankYellowInverse: {
    url: assetUrl("rbi-bank-yellow-neg.png"),
    ...BANK_LOCKUP,
    approvedOn: "Off-black surfaces only",
    officialName: "RBI-Logo-Bank-St-Yell-Neg-RGB",
  },
  bankMark: {
    url: assetUrl("rbi-bank-squared-col.png"),
    ...BANK_MARK,
    approvedOn: "Any surface — the squared mark carries its own yellow field",
    officialName: "RBI-Logo-Bank-St-Squared-Col-Neg-RGB",
  },
};

/** Official RGB values read directly out of the supplied logo artwork. */
export const rbiLogoArtworkColors = {
  primaryYellow: "#FDE500",
  offBlack: "#2B2D33",
  monoBlack: "#231F20",
} as const;

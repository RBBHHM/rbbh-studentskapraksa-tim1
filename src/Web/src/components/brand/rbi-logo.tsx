import type { ImgHTMLAttributes } from "react";

import { cn } from "@/lib/utils";
import { rbiLogoAssets, type RbiLogoVariant } from "@/design-system/foundations/logo-assets";

/** Optical sizes, not raw pixel heights — the lock-up keeps its aspect ratio. */
const heightClass = {
  xs: "h-5",
  sm: "h-6",
  md: "h-9",
  lg: "h-12",
  xl: "h-16",
} as const;

/** Accessible names per lock-up family, so alt text always matches the artwork. */
const altText: Record<RbiLogoVariant, string> = {
  colour: "Raiffeisen Bank International — Make it happen",
  colourInverse: "Raiffeisen Bank International — Make it happen",
  mono: "Raiffeisen Bank International — Make it happen",
  monoInverse: "Raiffeisen Bank International — Make it happen",
  yellowInverse: "Raiffeisen Bank International — Make it happen",
  bankMono: "Raiffeisen Bank",
  bankYellowInverse: "Raiffeisen Bank",
  bankMark: "Raiffeisen Bank",
};

export interface RbiLogoProps extends Omit<ImgHTMLAttributes<HTMLImageElement>, "src" | "alt" | "width" | "height"> {
  /**
   * Which approved lock-up to render. Pick the variant that matches the
   * surface — never recolour a variant with CSS filters.
   */
  variant?: RbiLogoVariant;
  size?: keyof typeof heightClass;
  /**
   * When the logo is the accessible name of a link (a header home link), set
   * `decorative` on the image and label the link instead, so the name is not
   * announced twice.
   */
  decorative?: boolean;
}

/**
 * Renders the official RBI "Make it happen" lock-up.
 *
 * The artwork is a supplied brand asset served as an image. It is deliberately
 * not reproduced as inline SVG paths or as text, and the component exposes no
 * colour props: the only way to change its appearance is to choose a different
 * approved variant.
 */
export function RbiLogo({ variant = "colour", size = "md", decorative = false, className, ...props }: RbiLogoProps) {
  const asset = rbiLogoAssets[variant];

  return (
    <img
      src={asset.url}
      width={asset.width}
      height={asset.height}
      alt={decorative ? "" : altText[variant]}
      aria-hidden={decorative || undefined}
      className={cn("w-auto object-contain", heightClass[size], className)}
      {...props}
    />
  );
}

import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

/**
 * Typography components.
 *
 * The brand voice is carried by Amalia: Black for display statements, Bold for
 * headings, Regular for prose. These components fix the size/weight/tracking
 * combinations that are approved, and keep the heading *level* independent of
 * the visual *size* so document outline order is never broken for the sake of
 * appearance.
 */

const displayVariants = cva("text-display font-brand text-balance", {
  variants: {
    size: {
      xl: "text-4xl sm:text-5xl xl:text-6xl",
      lg: "text-3xl sm:text-4xl xl:text-5xl",
      md: "text-2xl sm:text-3xl xl:text-4xl",
    },
  },
  defaultVariants: { size: "lg" },
});

export interface DisplayProps
  extends React.HTMLAttributes<HTMLHeadingElement>,
    VariantProps<typeof displayVariants> {
  as?: "h1" | "h2" | "h3" | "p" | "span" | "div";
}

export function Display({ as: Comp = "h1", size, className, ...props }: DisplayProps) {
  return <Comp className={cn(displayVariants({ size }), className)} {...props} />;
}

const headingVariants = cva("font-brand font-bold tracking-tight text-balance", {
  variants: {
    size: {
      1: "text-3xl sm:text-4xl leading-snug",
      2: "text-2xl sm:text-3xl leading-snug",
      3: "text-xl sm:text-2xl leading-snug",
      4: "text-lg sm:text-xl leading-snug",
      5: "text-base sm:text-lg leading-normal",
      6: "text-base leading-normal",
    },
  },
  defaultVariants: { size: 2 },
});

export interface HeadingProps
  extends React.HTMLAttributes<HTMLHeadingElement>,
    VariantProps<typeof headingVariants> {
  /** Document outline level. Choose this for structure. */
  level: 1 | 2 | 3 | 4 | 5 | 6;
  /** Visual size. Defaults to matching the level. */
  size?: 1 | 2 | 3 | 4 | 5 | 6;
}

export function Heading({ level, size, className, ...props }: HeadingProps) {
  const Comp = `h${level}` as const;
  return <Comp className={cn(headingVariants({ size: size ?? level }), className)} {...props} />;
}

const textVariants = cva("font-brand", {
  variants: {
    size: {
      xs: "text-xs leading-normal",
      sm: "text-sm leading-relaxed",
      md: "text-base leading-relaxed",
      lg: "text-lg leading-relaxed",
      xl: "text-xl font-light leading-relaxed",
    },
    tone: {
      primary: "text-text-primary",
      secondary: "text-text-secondary",
      tertiary: "text-text-tertiary",
      brand: "text-text-brand-accent",
      corporate: "text-text-corporate",
      danger: "text-feedback-danger",
      inherit: "",
    },
    weight: {
      light: "font-light",
      regular: "font-normal",
      medium: "font-medium",
      bold: "font-bold",
    },
  },
  defaultVariants: { size: "md", tone: "inherit" },
});

export interface TextProps
  extends React.HTMLAttributes<HTMLParagraphElement>,
    VariantProps<typeof textVariants> {
  as?: React.ElementType;
}

export function Text({ as: Comp = "p", size, tone, weight, className, ...props }: TextProps) {
  return <Comp className={cn(textVariants({ size, tone, weight }), className)} {...props} />;
}

export interface EyebrowProps extends React.HTMLAttributes<HTMLParagraphElement> {
  as?: React.ElementType;
}

/** Small all-caps label that sits above a section heading. */
export function Eyebrow({ as: Comp = "p", className, ...props }: EyebrowProps) {
  return <Comp className={cn("text-eyebrow font-brand text-text-secondary", className)} {...props} />;
}

/** Long-form prose wrapper used by the documentation pages. */
export function Prose({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "max-w-prose text-base leading-relaxed text-text-secondary",
        "[&_a]:text-text-link [&_a]:underline [&_a]:underline-offset-4 [&_a:hover]:text-text-link-hover",
        "[&_code]:rounded-xs [&_code]:bg-surface-sunken [&_code]:px-1.5 [&_code]:py-0.5 [&_code]:font-mono [&_code]:text-sm [&_code]:text-text-primary",
        "[&_h2]:mt-12 [&_h2]:mb-3 [&_h2]:text-2xl [&_h2]:font-bold [&_h2]:text-text-primary",
        "[&_h3]:mt-8 [&_h3]:mb-2 [&_h3]:text-xl [&_h3]:font-bold [&_h3]:text-text-primary",
        "[&_li]:mb-2 [&_ol]:mb-4 [&_ol]:list-decimal [&_ol]:pl-6",
        "[&_p]:mb-4 [&_strong]:font-bold [&_strong]:text-text-primary",
        "[&_ul]:mb-4 [&_ul]:list-disc [&_ul]:pl-6",
        className,
      )}
      {...props}
    />
  );
}

import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

/**
 * Layout primitives.
 *
 * Every page in the template composes these instead of hand-rolling padding
 * and max-widths, so page rhythm stays consistent and the 12-column grid and
 * gutter tokens are the single source of truth.
 */

const containerVariants = cva("mx-auto w-full px-4 lg:px-8", {
  variants: {
    width: {
      prose: "max-w-prose",
      narrow: "max-w-narrow",
      default: "max-w-default",
      wide: "max-w-wide",
      full: "max-w-none",
    },
  },
  defaultVariants: { width: "default" },
});

export interface ContainerProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof containerVariants> {
  as?: React.ElementType;
}

export function Container({ as: Comp = "div", width, className, ...props }: ContainerProps) {
  return <Comp className={cn(containerVariants({ width }), className)} {...props} />;
}

const sectionVariants = cva("", {
  variants: {
    spacing: {
      none: "",
      compact: "py-12",
      default: "py-16 lg:py-20",
      spacious: "py-20 lg:py-32",
    },
    surface: {
      default: "",
      subtle: "",
      muted: "",
      inverse: "",
      brand: "",
      corporate: "",
    },
  },
  defaultVariants: { spacing: "default", surface: "default" },
});

export interface SectionProps
  extends React.HTMLAttributes<HTMLElement>,
    VariantProps<typeof sectionVariants> {
  as?: React.ElementType;
}

/**
 * A full-bleed page section. `surface` sets the `data-surface` context, which
 * reassigns the semantic colour tokens for the whole subtree — this is why no
 * child component needs an "on dark" variant.
 */
export function Section({
  as: Comp = "section",
  spacing,
  surface = "default",
  className,
  ...props
}: SectionProps) {
  return (
    <Comp
      data-surface={surface === "default" ? undefined : surface}
      className={cn(
        sectionVariants({ spacing }),
        surface === "muted" && "bg-surface-muted",
        className,
      )}
      {...props}
    />
  );
}

const stackVariants = cva("flex", {
  variants: {
    direction: {
      column: "flex-col",
      row: "flex-row",
    },
    gap: {
      0: "gap-0",
      1: "gap-1",
      2: "gap-2",
      3: "gap-3",
      4: "gap-4",
      5: "gap-6",
      6: "gap-8",
      7: "gap-10",
      8: "gap-12",
    },
    align: {
      start: "items-start",
      center: "items-center",
      end: "items-end",
      stretch: "items-stretch",
      baseline: "items-baseline",
    },
    justify: {
      start: "justify-start",
      center: "justify-center",
      end: "justify-end",
      between: "justify-between",
    },
    wrap: {
      true: "flex-wrap",
    },
  },
  defaultVariants: { direction: "column", gap: 4 },
});

export interface StackProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof stackVariants> {
  as?: React.ElementType;
}

export function Stack({ as: Comp = "div", direction, gap, align, justify, wrap, className, ...props }: StackProps) {
  return (
    <Comp className={cn(stackVariants({ direction, gap, align, justify, wrap }), className)} {...props} />
  );
}

const gridVariants = cva("grid", {
  variants: {
    columns: {
      1: "grid-cols-1",
      2: "grid-cols-1 sm:grid-cols-2",
      3: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3",
      4: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-4",
      6: "grid-cols-2 sm:grid-cols-3 lg:grid-cols-6",
      12: "grid-cols-12",
    },
    gap: {
      tight: "gap-4",
      default: "gap-6",
      loose: "gap-8",
    },
  },
  defaultVariants: { columns: 3, gap: "default" },
});

export interface GridProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof gridVariants> {
  as?: React.ElementType;
}

export function Grid({ as: Comp = "div", columns, gap, className, ...props }: GridProps) {
  return <Comp className={cn(gridVariants({ columns, gap }), className)} {...props} />;
}

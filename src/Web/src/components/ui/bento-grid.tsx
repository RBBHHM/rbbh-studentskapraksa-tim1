import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

/**
 * Bento grid.
 *
 * A fixed 12-column track on desktop that collapses to a single column on
 * small screens, plus tiles that declare how many columns and rows they take.
 * Spans are expressed as tokens (a closed set of classes) rather than arbitrary
 * values, so the layout can be composed — and generated as copyable code by the
 * builder on the Patterns page — without ever emitting an ad-hoc class.
 */

const bentoGridVariants = cva("grid grid-cols-1 md:grid-cols-12", {
  variants: {
    gap: {
      tight: "gap-3",
      default: "gap-4",
      loose: "gap-6",
    },
    density: {
      /** Equal-height rows; tiles stretch to their row. */
      uniform: "md:auto-rows-[minmax(9rem,auto)]",
      /** Taller base row, for tiles carrying imagery or charts. */
      tall: "md:auto-rows-[minmax(12rem,auto)]",
    },
  },
  defaultVariants: { gap: "default", density: "uniform" },
});

export type BentoSpan = 3 | 4 | 6 | 8 | 9 | 12;
export type BentoRowSpan = 1 | 2;
export type BentoTone = "default" | "subtle" | "brand" | "corporate" | "inverse";

const spanClasses: Record<BentoSpan, string> = {
  3: "md:col-span-3",
  4: "md:col-span-4",
  6: "md:col-span-6",
  8: "md:col-span-8",
  9: "md:col-span-9",
  12: "md:col-span-12",
};

const rowSpanClasses: Record<BentoRowSpan, string> = {
  1: "md:row-span-1",
  2: "md:row-span-2",
};

/** Tone maps to a surface context, so children need no per-tone styling. */
const toneSurface: Record<BentoTone, string | undefined> = {
  default: undefined,
  subtle: "subtle",
  brand: "brand",
  corporate: "corporate",
  inverse: "inverse",
};

const toneClasses: Record<BentoTone, string> = {
  default: "border border-border-subtle bg-surface",
  subtle: "border border-border-subtle bg-surface-subtle",
  brand: "bg-surface-brand",
  corporate: "bg-surface-corporate",
  inverse: "bg-surface-inverse",
};

export interface BentoGridProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof bentoGridVariants> {}

export function BentoGrid({ gap, density, className, ...props }: BentoGridProps) {
  return <div className={cn(bentoGridVariants({ gap, density }), className)} {...props} />;
}

export interface BentoCardProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Columns out of 12 on desktop. Always full width below `md`. */
  readonly span?: BentoSpan;
  readonly rowSpan?: BentoRowSpan;
  readonly tone?: BentoTone;
  /** Renders the yellow accent rule along the top edge. */
  readonly accent?: boolean;
  readonly as?: React.ElementType;
}

export function BentoCard({
  span = 4,
  rowSpan = 1,
  tone = "default",
  accent = false,
  as: Comp = "div",
  className,
  children,
  ...props
}: BentoCardProps) {
  return (
    <Comp
      data-surface={toneSurface[tone]}
      className={cn(
        "relative flex min-w-0 flex-col overflow-hidden rounded-sm p-6",
        "transition-[border-color,box-shadow] duration-200 ease-standard",
        spanClasses[span],
        rowSpanClasses[rowSpan],
        toneClasses[tone],
        className,
      )}
      {...props}
    >
      {accent ? (
        <span aria-hidden="true" className="absolute inset-x-0 top-0 h-[3px] bg-surface-brand" />
      ) : null}
      {children}
    </Comp>
  );
}

/** Standard tile content: eyebrow, title, body, optional footer. */
export interface BentoCardBodyProps {
  readonly eyebrow?: string | undefined;
  readonly title: string;
  readonly body?: string | undefined;
  readonly footer?: React.ReactNode;
  /** Heading level, so the tile keeps the page outline intact. */
  readonly headingLevel?: 2 | 3 | 4;
}

export function BentoCardBody({ eyebrow, title, body, footer, headingLevel = 3 }: BentoCardBodyProps) {
  const Heading = `h${headingLevel}` as const;

  return (
    <>
      {eyebrow ? <p className="text-eyebrow text-text-tertiary">{eyebrow}</p> : null}
      <Heading className={cn("font-brand text-lg font-bold text-text-primary", eyebrow && "mt-2")}>
        {title}
      </Heading>
      {body ? <p className="mt-2 flex-1 text-sm text-text-secondary">{body}</p> : null}
      {footer ? <div className="mt-4">{footer}</div> : null}
    </>
  );
}

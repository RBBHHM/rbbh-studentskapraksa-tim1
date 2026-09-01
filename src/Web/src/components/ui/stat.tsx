import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { ArrowDownRight, ArrowUpRight, Minus, type LucideIcon } from "lucide-react";

import { cn } from "@/lib/utils";

/**
 * Statistic (KPI) component.
 *
 * The figure is the hero: Amalia Black with tabular numerals so a column of
 * numbers stays aligned. `variant` covers the four approved treatments —
 * a yellow top rule, a bordered card, a filled panel and plain type — and each
 * one inherits its colours from the surrounding `data-surface` context, so the
 * same component works on light, subtle, brand, corporate and inverse sections
 * without a per-surface variant.
 */

const statVariants = cva("min-w-0", {
  variants: {
    variant: {
      /** Yellow rule above the figure; the default editorial treatment. */
      rule: "border-t-3 border-t-border-brand pt-4",
      /** Bordered tile — use inside dashboards and metric bands. */
      card: "rounded-sm border border-border-subtle border-t-3 border-t-border-brand bg-surface p-4",
      /** Filled tile for a quieter grid where the border grid would be noisy. */
      panel: "rounded-sm bg-surface-subtle p-4",
      /** Type only, for inline use inside prose or a table footer. */
      plain: "",
    },
    align: {
      start: "text-left",
      center: "text-center",
    },
    size: {
      sm: "[--stat-value-size:var(--font-size-2xl)]",
      md: "[--stat-value-size:var(--font-size-4xl)]",
      lg: "[--stat-value-size:var(--font-size-5xl)]",
    },
  },
  defaultVariants: { variant: "rule", align: "start", size: "md" },
});

export type StatTrendDirection = "up" | "down" | "flat";

const trendIcons: Record<StatTrendDirection, LucideIcon> = {
  up: ArrowUpRight,
  down: ArrowDownRight,
  flat: Minus,
};

const trendTones: Record<StatTrendDirection, string> = {
  up: "text-[color:var(--feedback-success-foreground)]",
  down: "text-[color:var(--feedback-danger-foreground)]",
  flat: "text-text-tertiary",
};

export interface StatTrend {
  readonly direction: StatTrendDirection;
  /** Formatted delta, e.g. `+4.2%`. */
  readonly value: string;
  /** What the delta is measured against, e.g. `vs. last quarter`. */
  readonly caption?: string | undefined;
}

export interface StatProps
  extends Omit<React.HTMLAttributes<HTMLDivElement>, "children">,
    VariantProps<typeof statVariants> {
  readonly label: string;
  readonly value: string;
  readonly hint?: string | undefined;
  readonly icon?: LucideIcon | undefined;
  readonly trend?: StatTrend | undefined;
}

export function Stat({
  label,
  value,
  hint,
  icon: Icon,
  trend,
  variant,
  align,
  size,
  className,
  ...props
}: StatProps) {
  const TrendIcon = trend ? trendIcons[trend.direction] : undefined;

  return (
    <div className={cn(statVariants({ variant, align, size }), className)} {...props}>
      <div className="flex items-start justify-between gap-3">
        <p className="text-eyebrow text-text-tertiary">{label}</p>
        {Icon ? <Icon aria-hidden="true" className="size-4 shrink-0 text-text-tertiary" /> : null}
      </div>

      <p
        className="mt-3 font-brand font-black tabular-nums text-text-primary"
        style={{ fontSize: "var(--stat-value-size)", lineHeight: 1.05 }}
      >
        {value}
      </p>

      {trend ? (
        <p className={cn("mt-2 flex items-center gap-1.5 text-sm font-medium", trendTones[trend.direction])}>
          {TrendIcon ? <TrendIcon aria-hidden="true" className="size-4 shrink-0" /> : null}
          <span className="tabular-nums">{trend.value}</span>
          {trend.caption ? <span className="font-normal text-text-tertiary">{trend.caption}</span> : null}
        </p>
      ) : null}

      {hint ? <p className="mt-2 text-sm text-text-secondary">{hint}</p> : null}
    </div>
  );
}

const statGroupVariants = cva("grid", {
  variants: {
    columns: {
      2: "grid-cols-1 sm:grid-cols-2",
      3: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3",
      4: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-4",
    },
    gap: {
      tight: "gap-4",
      default: "gap-6",
      loose: "gap-8",
    },
  },
  defaultVariants: { columns: 4, gap: "default" },
});

export interface StatGroupProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof statGroupVariants> {
  /** Accessible name for the band, rendered visually hidden. */
  readonly label?: string | undefined;
}

/** A row of statistics. Rendered as a labelled group so the set is announced as one. */
export function StatGroup({ label, columns, gap, className, children, ...props }: StatGroupProps) {
  return (
    <section aria-label={label} className={cn(statGroupVariants({ columns, gap }), className)} {...props}>
      {children}
    </section>
  );
}

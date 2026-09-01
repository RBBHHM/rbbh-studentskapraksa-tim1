import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

/**
 * Status badge.
 *
 * Status is never communicated by colour alone: each tone renders a leading
 * dot *and* text, and the surrounding component is expected to supply an
 * icon or a visually hidden label where the badge is the only signal.
 */
const badgeVariants = cva(
  "inline-flex items-center gap-1.5 rounded-pill border px-2.5 py-0.5 text-xs font-medium whitespace-nowrap",
  {
    variants: {
      tone: {
        neutral: "border-feedback-neutral-border bg-feedback-neutral-bg text-feedback-neutral",
        info: "border-feedback-info-border bg-feedback-info-bg text-feedback-info",
        success: "border-feedback-success-border bg-feedback-success-bg text-feedback-success",
        warning: "border-feedback-warning-border bg-feedback-warning-bg text-feedback-warning",
        danger: "border-feedback-danger-border bg-feedback-danger-bg text-feedback-danger",
        brand: "border-border-brand bg-surface-brand-subtle text-text-primary",
        corporate: "border-border-corporate bg-surface-corporate-subtle text-text-corporate",
      },
      variant: {
        subtle: "",
        solid: "",
        outline: "bg-transparent",
      },
    },
    compoundVariants: [
      { tone: "brand", variant: "solid", class: "bg-surface-brand text-text-on-brand border-transparent" },
      {
        tone: "corporate",
        variant: "solid",
        class: "bg-surface-corporate text-text-on-corporate border-transparent",
      },
      { tone: "danger", variant: "solid", class: "bg-feedback-danger text-text-inverse border-transparent" },
      { tone: "neutral", variant: "solid", class: "bg-surface-inverse text-text-inverse border-transparent" },
    ],
    defaultVariants: { tone: "neutral", variant: "subtle" },
  },
);

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement>, VariantProps<typeof badgeVariants> {
  /** Renders a leading dot so the tone is not the only differentiator. */
  withDot?: boolean;
}

export function Badge({ className, tone, variant, withDot = false, children, ...props }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ tone, variant }), className)} {...props}>
      {withDot ? <span aria-hidden="true" className="size-1.5 rounded-pill bg-current" /> : null}
      {children}
    </span>
  );
}

export { badgeVariants };

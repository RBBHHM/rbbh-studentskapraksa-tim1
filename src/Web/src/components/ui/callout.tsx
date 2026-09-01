import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { AlertTriangle, CheckCircle2, Info, OctagonAlert, type LucideIcon } from "lucide-react";

import { cn } from "@/lib/utils";

/**
 * Callout — an inline message block.
 *
 * Each tone pairs a colour with a distinct icon so the meaning survives
 * greyscale printing and colour-vision deficiency. `danger` renders as an
 * assertive live region because errors need to interrupt; the others are
 * polite or inert.
 */
const calloutVariants = cva("flex gap-3 border-l-6 p-4 text-sm", {
  variants: {
    tone: {
      info: "border-l-feedback-info bg-feedback-info-bg text-text-primary",
      success: "border-l-feedback-success bg-feedback-success-bg text-text-primary",
      warning: "border-l-feedback-warning bg-feedback-warning-bg text-text-primary",
      danger: "border-l-feedback-danger bg-feedback-danger-bg text-text-primary",
      brand: "border-l-border-brand bg-surface-brand-faint text-text-primary",
    },
  },
  defaultVariants: { tone: "info" },
});

const toneIcon: Record<NonNullable<VariantProps<typeof calloutVariants>["tone"]>, LucideIcon> = {
  info: Info,
  success: CheckCircle2,
  warning: AlertTriangle,
  danger: OctagonAlert,
  brand: Info,
};

const toneIconClass: Record<NonNullable<VariantProps<typeof calloutVariants>["tone"]>, string> = {
  info: "text-feedback-info",
  success: "text-feedback-success",
  warning: "text-feedback-warning",
  danger: "text-feedback-danger",
  brand: "text-text-brand-accent",
};

export interface CalloutProps extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof calloutVariants> {
  title?: string;
}

export function Callout({ className, tone = "info", title, children, ...props }: CalloutProps) {
  const resolved = tone ?? "info";
  const Icon = toneIcon[resolved];

  return (
    <div
      role={resolved === "danger" ? "alert" : "note"}
      className={cn(calloutVariants({ tone: resolved }), className)}
      {...props}
    >
      <Icon aria-hidden="true" className={cn("mt-0.5 size-6 shrink-0", toneIconClass[resolved])} />
      <div className="min-w-0 space-y-1">
        {title ? <p className="font-bold text-text-primary">{title}</p> : null}
        <div className="text-text-secondary [&_p]:mb-0">{children}</div>
      </div>
    </div>
  );
}

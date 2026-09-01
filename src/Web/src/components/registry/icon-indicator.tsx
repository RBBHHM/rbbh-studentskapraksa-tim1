import {
  BriefcaseBusiness,
  CalendarDays,
  CalendarRange,
  Check,
  CircleMinus,
  Clock3,
  Globe2,
  Home,
  Link2,
  ShieldCheck,
  UsersRound,
  X,
} from "lucide-react";

export type IndicatorKind =
  | "active"
  | "inactive"
  | "yes"
  | "no"
  | "verified"
  | "draft"
  | "rejected"
  | "employee"
  | "family"
  | "related"
  | "resident"
  | "nonresident"
  | "daily"
  | "monthly";

const indicators = {
  active: { icon: Check, className: "border-feedback-success-border bg-feedback-success-bg text-feedback-success" },
  // Keep inactive values unmistakable in both themes. They remain in the
  // administrative table, but are visually distinct from active values.
  inactive: { icon: CircleMinus, className: "border-feedback-warning-border bg-feedback-warning-bg text-feedback-warning" },
  yes: { icon: Check, className: "border-feedback-success-border bg-feedback-success-bg text-feedback-success" },
  no: { icon: X, className: "border-border-strong bg-surface-muted text-text-secondary" },
  verified: { icon: ShieldCheck, className: "border-feedback-success-border bg-feedback-success-bg text-feedback-success" },
  draft: { icon: Clock3, className: "border-feedback-info-border bg-feedback-info-bg text-feedback-info" },
  rejected: { icon: X, className: "border-feedback-danger-border bg-feedback-danger-bg text-feedback-danger" },
  employee: { icon: BriefcaseBusiness, className: "border-feedback-info-border bg-feedback-info-bg text-feedback-info" },
  family: { icon: UsersRound, className: "border-border-brand bg-surface-brand-subtle text-text-brand-accent" },
  related: { icon: Link2, className: "border-border-strong bg-surface-muted text-text-primary" },
  resident: { icon: Home, className: "border-feedback-success-border bg-feedback-success-bg text-feedback-success" },
  nonresident: { icon: Globe2, className: "border-feedback-info-border bg-feedback-info-bg text-feedback-info" },
  daily: { icon: CalendarDays, className: "border-feedback-info-border bg-feedback-info-bg text-feedback-info" },
  monthly: { icon: CalendarRange, className: "border-border-brand bg-surface-brand-subtle text-text-brand-accent" },
} as const;

export function IconIndicator({ kind, label, size = "md" }: { readonly kind: IndicatorKind; readonly label: string; readonly size?: "sm" | "md" }) {
  const definition = indicators[kind];
  const Icon = definition.icon;
  return (
    <span
      role="img"
      aria-label={label}
      title={label}
      className={`inline-flex shrink-0 items-center justify-center rounded-full border ${size === "sm" ? "size-7" : "size-8"} ${definition.className}`}
    >
      <Icon className={size === "sm" ? "size-3.5" : "size-4"} aria-hidden="true" />
    </span>
  );
}

import type { LucideIcon } from "lucide-react";

import { Stat, type StatTrend } from "@/components/ui/stat";

/**
 * Metric tile used by the admin band.
 *
 * A thin alias over `Stat` in its `card` variant, so the admin screen and the
 * documented statistic component can never drift apart.
 */
export interface MetricCardProps {
  readonly label: string;
  readonly value: string;
  readonly hint?: string | undefined;
  readonly icon?: LucideIcon | undefined;
  readonly trend?: StatTrend | undefined;
  readonly className?: string | undefined;
}

export function MetricCard({ label, value, hint, icon, trend, className }: MetricCardProps) {
  return (
    <Stat
      variant="card"
      size="sm"
      label={label}
      value={value}
      hint={hint}
      icon={icon}
      trend={trend}
      className={className}
    />
  );
}

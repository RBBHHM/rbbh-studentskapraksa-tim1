import * as React from "react";

import { cn } from "@/lib/utils";

/**
 * Centred dock.
 *
 * A floating, pill-shaped switcher that hugs its own content instead of
 * stretching across a full-width container — the iOS dock model. Used wherever
 * a small set of sibling views share one screen (the application examples, the
 * catalog section jumper on small screens).
 */
export function Dock({
  label,
  className,
  children,
}: {
  label: string;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={cn("pointer-events-none flex justify-center px-4", className)}>
      <nav
        aria-label={label}
        className={cn(
          "glass-strong pointer-events-auto max-w-full overflow-x-auto rounded-full",
          "border border-border-subtle p-1 shadow-md",
          "[-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden",
        )}
      >
        <ul className="flex items-center gap-1">{children}</ul>
      </nav>
    </div>
  );
}

/** Shared item styling for dock links and anchors. */
export function dockItemClasses(active: boolean) {
  return cn(
    "inline-flex h-9 shrink-0 items-center gap-2 rounded-full px-3.5 text-sm font-medium whitespace-nowrap",
    "transition-colors duration-150 ease-standard focus-ring",
    active
      ? "bg-surface-brand font-bold text-text-on-brand"
      : "text-text-secondary hover:bg-surface-muted hover:text-text-primary",
  );
}

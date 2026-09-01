import * as React from "react";
import * as TabsPrimitive from "@radix-ui/react-tabs";

import { cn } from "@/lib/utils";

/**
 * Tabs — RBI underline treatment.
 *
 * There is deliberately no filled container behind the tab row: a large muted
 * block competes with the content it introduces. Instead the row sits on a
 * hairline rule and the selected tab carries a 3px yellow indicator, which is
 * the same selected-state language the primary navigation uses.
 *
 * The list scrolls horizontally rather than wrapping on narrow viewports, so a
 * tab set never reflows into two rows on a phone.
 */

const Tabs = TabsPrimitive.Root;

const TabsList = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.List>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.List>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.List
    ref={ref}
    className={cn(
      "relative flex w-full items-stretch gap-1 overflow-x-auto border-b border-border-subtle",
      "[-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden",
      className,
    )}
    {...props}
  />
));
TabsList.displayName = TabsPrimitive.List.displayName;

const TabsTrigger = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.Trigger>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Trigger
    ref={ref}
    className={cn(
      "group relative inline-flex min-h-11 shrink-0 cursor-pointer items-center justify-center",
      "px-3 pb-2.5 pt-2 text-sm font-medium whitespace-nowrap",
      "text-text-secondary transition-colors duration-150 ease-standard",
      "hover:text-text-primary",
      "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[color:var(--focus-ring-color)]",
      "disabled:cursor-not-allowed disabled:text-text-disabled",
      "data-[state=active]:font-bold data-[state=active]:text-text-primary",
      "after:absolute after:inset-x-1 after:-bottom-px after:h-[3px] after:origin-left after:scale-x-0",
      "after:bg-surface-brand after:transition-transform after:duration-150 after:ease-standard",
      "data-[state=active]:after:scale-x-100",
      className,
    )}
    {...props}
  />
));
TabsTrigger.displayName = TabsPrimitive.Trigger.displayName;

const TabsContent = React.forwardRef<
  React.ElementRef<typeof TabsPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Content>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Content
    ref={ref}
    className={cn(
      "mt-6 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[color:var(--focus-ring-color)]",
      className,
    )}
    {...props}
  />
));
TabsContent.displayName = TabsPrimitive.Content.displayName;

export { Tabs, TabsList, TabsTrigger, TabsContent };

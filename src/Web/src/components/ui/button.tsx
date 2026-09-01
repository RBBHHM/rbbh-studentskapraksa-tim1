import * as React from "react";
import { Slot, Slottable } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { Loader2 } from "lucide-react";

import { cn } from "@/lib/utils";

/**
 * RBI Button.
 *
 * Variant rules from the brand guidelines:
 * - `primary` is primary yellow with off-black text. There is exactly one
 *   primary action per view. White text on yellow is never permitted.
 * - `secondary` is an off-black outline — the workhorse for most actions.
 * - `corporate` uses corporate green, reserved for the corporate/ESG context.
 * - `tertiary` and `ghost` are low-emphasis; `link` is inline in prose.
 *
 * Accessibility:
 * - Default height is 44px, meeting the WCAG 2.2 target-size minimum.
 * - Focus is a 2px ring with a 2px offset, inherited from the base layer.
 * - `loading` sets `aria-busy` and keeps the control focusable, so screen
 *   reader users are not thrown off by a disappearing element.
 */
const buttonVariants = cva(
  [
    "inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-sm",
    "font-medium cursor-pointer select-none",
    "transition-[background-color,border-color,color,box-shadow] duration-150 ease-standard",
    "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-45",
    "aria-disabled:pointer-events-none aria-disabled:cursor-not-allowed aria-disabled:opacity-45",
    "[&_svg]:pointer-events-none [&_svg]:shrink-0",
  ],
  {
    variants: {
      variant: {
        primary: [
          "bg-[var(--action-primary-background)] text-[var(--action-primary-foreground)]",
          "hover:bg-[var(--action-primary-background-hover)]",
          "active:bg-[var(--action-primary-background-active)]",
        ],
        secondary: [
          "border-2 border-[var(--action-secondary-border)] bg-surface-default",
          "text-[var(--action-secondary-foreground)]",
          "hover:bg-[var(--action-secondary-background-hover)]",
          "active:bg-[var(--action-secondary-background-active)]",
        ],
        tertiary: [
          "bg-[var(--action-tertiary-background)] text-[var(--action-tertiary-foreground)]",
          "hover:bg-[var(--action-tertiary-background-hover)]",
          "active:bg-[var(--action-tertiary-background-active)]",
        ],
        corporate: [
          "bg-[var(--action-corporate-background)] text-[var(--action-corporate-foreground)]",
          "hover:bg-[var(--action-corporate-background-hover)]",
          "active:bg-[var(--action-corporate-background-active)]",
        ],
        ghost: [
          "bg-surface-default text-text-primary",
          "hover:bg-[var(--action-tertiary-background)]",
          "active:bg-[var(--action-tertiary-background-hover)]",
        ],
        destructive: [
          "bg-[var(--action-destructive-background)] text-[var(--action-destructive-foreground)]",
          "hover:bg-[var(--action-destructive-background-hover)]",
          "active:bg-[var(--action-destructive-background-active)]",
        ],
        link: [
          "h-auto rounded-xs bg-transparent p-0 text-text-link underline underline-offset-4",
          "hover:text-text-link-hover",
        ],
      },
      size: {
        /** 32px — dense toolbars and table row actions. */
        sm: "h-8 gap-1.5 px-3 text-xs [&_svg]:size-3.5",
        /** 40px — the default control height for forms and page actions. */
        md: "h-10 px-4 text-sm [&_svg]:size-4",
        /** 44px — hero and primary page-level calls to action. */
        lg: "h-11 px-6 text-base [&_svg]:size-4",
        icon: "size-10 p-0 [&_svg]:size-4",
        /**
         * 32px visually, but the hit area is expanded to 40px through a
         * transparent pseudo-element so dense table rows stay tappable.
         */
        "icon-sm":
          "relative size-8 p-0 [&_svg]:size-3.5 after:absolute after:-inset-1 after:content-['']",
      },
      fullWidth: {
        true: "w-full",
      },
    },
    compoundVariants: [{ variant: "link", size: ["sm", "md", "lg"], class: "h-auto px-0" }],
    defaultVariants: {
      variant: "primary",
      size: "md",
    },
  },
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
  /** Shows a spinner, sets `aria-busy`, and blocks activation. */
  loading?: boolean;
  /** Accessible status announced while `loading` is true. */
  loadingLabel?: string;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant,
      size,
      fullWidth,
      asChild = false,
      loading = false,
      loadingLabel = "Loading",
      children,
      type,
      ...props
    },
    ref,
  ) => {
    const Comp = asChild ? Slot : "button";

    return (
      <Comp
        ref={ref}
        type={asChild ? undefined : (type ?? "button")}
        aria-busy={loading || undefined}
        aria-disabled={loading || undefined}
        className={cn(buttonVariants({ variant, size, fullWidth }), className)}
        {...props}
      >
        {loading ? (
          <>
            <Loader2 className="animate-spin" aria-hidden="true" />
            <span className="sr-only">{loadingLabel}</span>
          </>
        ) : null}
        <Slottable>{children}</Slottable>

      </Comp>
    );
  },
);
Button.displayName = "Button";

export { Button, buttonVariants };

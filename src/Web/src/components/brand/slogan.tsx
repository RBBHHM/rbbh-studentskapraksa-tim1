import * as React from "react";
import { useTranslation } from "react-i18next";

import { cn } from "@/lib/utils";

/**
 * The brand claim, set the way RBI sets it.
 *
 * In the print lock-up the claim is a three-part statement: two words carry it
 * and the middle word recedes — set in a light cut, at a slightly smaller
 * optical size. The wording itself is localization content (`common.brand.slogan`)
 * so it can be changed through a localization release, never in a component.
 */

export interface SloganProps extends React.HTMLAttributes<HTMLElement> {
  as?: "span" | "p" | "div" | "h1" | "h2";
  /** Renders the claim with a trailing full stop, as in the print lock-up. */
  withStop?: boolean;
}

/** Slot order of the claim. The middle slot is always the recessive one. */
const SLOTS = ["lead", "accent", "tail"] as const;

export function Slogan({ as: Comp = "span", withStop = false, className, ...props }: SloganProps) {
  const { t } = useTranslation("common");

  const words = SLOTS.map((slot) => t(`brand.slogan.${slot}` as never));

  return (
    <Comp className={cn("inline font-normal tracking-tight", className)} {...props}>
      {words.map((word, index) => (
        <React.Fragment key={SLOTS[index]}>
          {index > 0 ? " " : null}
          <span
            className={
              SLOTS[index] === "accent"
                ? "font-light text-[0.86em] tracking-normal text-text-primary/65"
                : "font-normal"
            }
          >
            {word}
          </span>
        </React.Fragment>
      ))}
      {withStop ? <span className="font-normal">.</span> : null}
    </Comp>
  );
}

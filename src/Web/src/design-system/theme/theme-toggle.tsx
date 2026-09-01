import { Moon, Sun } from "lucide-react";
import { useTranslation } from "react-i18next";

import type { TranslationKey } from "@/localization";
import { cn } from "@/lib/utils";

import { useTheme, type Theme } from "./use-theme";

type CommonKey = TranslationKey<"common">;

const options: { value: Exclude<Theme, "system">; icon: typeof Sun; labelKey: CommonKey }[] = [
  { value: "light", icon: Sun, labelKey: "theme.light" },
  { value: "dark", icon: Moon, labelKey: "theme.dark" },
];

/**
 * Segmented light/dark theme control. Each
 * option is a labelled, keyboard-reachable button; the active one carries
 * `aria-pressed` so the current theme is never conveyed by colour alone.
 */
export function ThemeToggle({ className }: { className?: string }) {
  const { resolvedTheme, setTheme } = useTheme();
  const { t } = useTranslation("common");

  return (
    <div
      role="group"
      aria-label={t("theme.switcherAria")}
      className={cn(
        "flex shrink-0 items-center gap-0.5 rounded-sm border border-border-subtle p-0.5",
        className,
      )}
    >
      {options.map(({ value, icon: Icon, labelKey }) => {
        const active = resolvedTheme === value;
        return (
          <button
            key={value}
            type="button"
            aria-pressed={active}
            aria-label={t(labelKey)}
            title={t(labelKey)}
            onClick={() => setTheme(value)}
            className={cn(
              "flex size-8 items-center justify-center rounded-xs bg-surface-raised transition-colors duration-150 ease-standard",
              "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[color:var(--focus-ring-color)]",
              active
                ? "bg-surface-brand text-text-on-brand"
                : "text-text-tertiary hover:bg-surface-muted hover:text-text-primary",
            )}
          >
            <Icon aria-hidden="true" className="size-4" />
          </button>
        );
      })}
    </div>
  );
}

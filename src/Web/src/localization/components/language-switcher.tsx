import { Languages } from "lucide-react";
import { useTranslation } from "react-i18next";

import { cn } from "@/lib/utils";

import { getFormatters } from "../hooks/use-formatters";
import { useLocalizedNavigation } from "../hooks/use-localized-navigation";
import { useLocalization } from "../providers/localization-provider";

/**
 * RBI-branded language switcher.
 *
 * Options come from the runtime manifest, so publishing a new language makes it
 * selectable without a frontend deployment. No flags (a language is not a
 * country); display names come from the localization bundle with an
 * `Intl.DisplayNames` fallback, and the locale code is only ever a hint.
 */
export function LanguageSwitcher({ className }: { className?: string }) {
  const { availableLocales, locale, status } = useLocalization();
  const { changeLocale } = useLocalizedNavigation();
  const { t } = useTranslation("common");

  if (status !== "ready" || availableLocales.length <= 1) return null;

  const labelFor = (code: string, displayNameKey: string) => {
    const translated = t(displayNameKey, { defaultValue: "" });
    if (translated) return translated;
    return getFormatters(locale).formatLocaleName(code);
  };

  return (
    <div className={cn("flex min-w-0 shrink-0 items-center gap-1.5", className)}>
      <Languages aria-hidden="true" className="hidden size-4 shrink-0 text-text-tertiary sm:block" />
      <label className="sr-only" htmlFor="language-switcher">
        {t("language.switcherAria")}
      </label>
      <select
        id="language-switcher"
        value={locale}
        onChange={(event) => changeLocale(event.target.value)}
        className={cn(
          "h-9 min-w-0 max-w-[10rem] rounded-sm border border-border-subtle bg-surface-raised px-2 text-sm font-medium",
          "text-text-primary transition-colors duration-150 ease-standard hover:border-border-default",
          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[color:var(--focus-ring-color)]",
        )}
      >
        {availableLocales.map((entry) => (
          <option key={entry.code} value={entry.code}>
            {labelFor(entry.code, entry.displayNameKey)}
          </option>
        ))}
      </select>
    </div>
  );
}

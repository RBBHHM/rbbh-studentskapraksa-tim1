import { Link } from "@tanstack/react-router";
import { Menu, X } from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";

import { RbiLogo } from "@/components/brand/rbi-logo";
import { AuthorSignature } from "@/components/brand/signature";
import { Container } from "@/components/layout/primitives";
import { Button } from "@/components/ui/button";
import { LanguageSwitcher } from "@/localization";
import { ThemeToggle, useTheme } from "@/design-system/theme";
import type { TranslationKey } from "@/localization";
import { cn } from "@/lib/utils";

/**
 * Application shell: skip link, header, primary navigation, footer.
 *
 * Every visible string is a translation key resolved at runtime from the active
 * localization release, so wording changes ship without touching this file and
 * without a deployment. Nothing here is hardcoded copy.
 *
 * Navigation semantics:
 * - The header home link carries the accessible name; the logo image inside it
 *   is decorative so the name is announced once.
 * - The current page is marked with `aria-current="page"` in addition to the
 *   yellow indicator, so the state is not conveyed by colour alone.
 * - The skip link is the first focusable element and targets `#main-content`.
 *
 * Motion: hover changes colour only and the selected indicator is a 3px rule
 * inset inside the item's own box, so nothing paints outside the header or over
 * the content beneath it. The header itself is fully opaque above every sticky
 * element on the page.
 */

type NavigationKey = TranslationKey<"navigation">;
type CommonKey = TranslationKey<"common">;

const navigation = [
  { to: "/", labelKey: "primary.overview" },
  { to: "/foundations", labelKey: "primary.foundations" },
  { to: "/components", labelKey: "primary.components" },
  { to: "/patterns", labelKey: "primary.patterns" },
  { to: "/applications", labelKey: "primary.applications" },
  { to: "/architecture", labelKey: "primary.architecture" },
] as const satisfies readonly { to: string; labelKey: NavigationKey }[];

/** Footer link groups, derived from the single navigation source above. */
const footerGroups = [
  { headingKey: "footer.groups.designSystem", items: ["/foundations", "/components", "/patterns"] },
  { headingKey: "footer.groups.engineering", items: ["/applications", "/architecture"] },
] as const satisfies readonly { headingKey: CommonKey; items: readonly string[] }[];

const navigationByPath = new Map(navigation.map((item) => [item.to, item] as const));

export function AppShell({ children }: { children: React.ReactNode }) {
  const { t } = useTranslation("navigation");
  const { t: tCommon } = useTranslation("common");
  const { resolvedTheme } = useTheme();

  const [mobileOpen, setMobileOpen] = useState(false);

  /** Lock background scroll while the mobile panel covers the viewport. */
  useEffect(() => {
    if (!mobileOpen) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, [mobileOpen]);

  return (
    <div className="flex min-h-screen flex-col bg-surface">
      <a
        href="#main-content"
        className={cn(
          "sr-only focus:not-sr-only",
          "focus:fixed focus:top-4 focus:left-4 focus:z-90 focus:rounded-sm",
          "focus:bg-surface-inverse focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:text-text-inverse",
        )}
      >
        {t("shell.skipToContent")}
      </a>

      <header className="glass-strong sticky top-0 z-50 overflow-hidden border-b border-border-subtle">
        <Container
          width="wide"
          className="grid h-16 grid-cols-[auto_minmax(0,1fr)] items-center gap-3 xl:h-18 xl:grid-cols-[auto_minmax(0,1fr)_auto] xl:gap-6"
        >
          <Link
            to="/"
            aria-label={t("shell.home")}
            className="flex min-w-0 items-center rounded-xs"
          >
            <RbiLogo
              variant={resolvedTheme === "dark" ? "bankYellowInverse" : "bankMono"}
              size="sm"
              decorative
              className="xl:h-7"
            />
          </Link>

          <nav aria-label={t("shell.primary")} className="hidden min-w-0 self-stretch xl:block">
            <ul className="flex h-full items-stretch">
              {navigation.map((item) => (
                <li key={item.to} className="flex">
                  <Link
                    to={item.to as never}
                    activeOptions={{ exact: item.to === "/" }}
                    className={cn(
                      "relative flex items-center px-3 text-sm font-medium whitespace-nowrap",
                      "text-text-secondary transition-colors duration-150 ease-standard",
                      "hover:text-text-primary",
                      // The indicator is a 3px rule flush with the header's own
                      // bottom edge, so it never paints over page content.
                      "after:absolute after:inset-x-0 after:bottom-0 after:h-[3px]",
                      "after:bg-transparent after:transition-colors after:duration-150 after:ease-standard",
                      "hover:after:bg-border-default",
                    )}
                    activeProps={{
                      "aria-current": "page",
                      className:
                        "font-bold text-text-primary after:bg-surface-brand hover:after:bg-surface-brand",
                    }}
                  >
                    {t(item.labelKey)}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
          <div className="flex items-center justify-self-end gap-2">
            <ThemeToggle className="hidden sm:flex" />
            <LanguageSwitcher />

            <Button
              variant="ghost"
              size="icon"
              className="xl:hidden"
              aria-expanded={mobileOpen}
              aria-controls="mobile-navigation"
              aria-label={mobileOpen ? t("shell.closeMenu") : t("shell.openMenu")}
              onClick={() => setMobileOpen((open) => !open)}
            >
              {mobileOpen ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}
            </Button>
          </div>
        </Container>
      </header>

      {mobileOpen ? (
        <div className="fixed inset-x-0 top-16 bottom-0 z-40 xl:hidden">
          <button
            type="button"
            tabIndex={-1}
            aria-hidden="true"
            onClick={() => setMobileOpen(false)}
            className="absolute inset-0 bg-surface-inverse/40"
          />
          <nav
            id="mobile-navigation"
            aria-label={t("shell.primary")}
            className="glass-strong relative max-h-full overflow-y-auto border-b border-border-subtle shadow-lg"
          >
            <Container width="wide" className="py-3">
              <ul className="flex flex-col divide-y divide-border-subtle">
                {navigation.map((item) => (
                  <li key={item.to}>
                    <Link
                      to={item.to as never}
                      activeOptions={{ exact: item.to === "/" }}
                      onClick={() => setMobileOpen(false)}
                      className="flex min-h-14 items-center rounded-xs pl-3 text-base font-medium text-text-secondary"
                      activeProps={{
                        "aria-current": "page",
                        className: "border-l-3 border-l-border-brand font-bold text-text-primary",
                      }}
                    >
                      {t(item.labelKey)}
                    </Link>
                  </li>
                ))}
              </ul>
            </Container>
          </nav>
        </div>
      ) : null}

      <main id="main-content" className="flex-1">
        {children}
      </main>

      <footer data-surface="inverse" className="mt-auto">
        <Container width="wide" className="py-16 lg:py-20">
          <div className="flex flex-col gap-12 lg:flex-row lg:items-start lg:justify-between lg:gap-32">
            <div className="max-w-sm space-y-4">
              <RbiLogo variant="colourInverse" size="md" decorative />
              <p className="text-sm text-text-secondary">{tCommon("footer.blurb")}</p>
            </div>

            <div className="grid gap-12 sm:grid-cols-2 sm:gap-32">
              {footerGroups.map((group) => (
                <div key={group.headingKey}>
                  <h2 className="text-eyebrow mb-3 text-text-secondary">
                    {tCommon(group.headingKey)}
                  </h2>
                  <ul className="space-y-2 text-sm">
                    {group.items.map((path) => {
                      const item = navigationByPath.get(path);
                      if (!item) return null;
                      return (
                        <li key={item.to}>
                          <Link
                            to={item.to as never}
                            className="text-text-primary underline-offset-4 hover:underline"
                          >
                            {t(item.labelKey)}
                          </Link>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              ))}
            </div>
          </div>

          <AuthorSignature tone="inverse" className="mt-16 max-w-2xl" />

          <div className="mt-12 border-t border-border-default pt-8">
            <p className="text-xs text-text-tertiary">{tCommon("footer.legal")}</p>
          </div>
        </Container>
      </footer>
    </div>
  );
}

import { Link, Outlet, useRouterState } from "@tanstack/react-router";
import { BookOpenCheck, LogOut, Menu, UserRound, X } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";

import { RbiLogo } from "@/components/brand/rbi-logo";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/design-system/theme/theme-toggle";
import { LanguageSwitcher } from "@/localization";
import { isAuthenticationConfigured, keycloak } from "@/lib/auth/keycloak";
import { registryResources } from "@/lib/registry/resources";
import { hasAllApplicationAccesses, hasApplicationAccess } from "@/lib/auth/application-access";

export function RegistryShell() {
  const { t } = useTranslation("registry");
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const [open, setOpen] = useState(false);

  return (
    <div className="min-h-screen bg-surface text-text-primary">
      <header className="sticky top-0 z-40 border-b border-border-subtle bg-surface-raised shadow-sm">
        <div className="flex h-16 w-full items-center gap-4 px-4 lg:px-6">
          <Link
            to="/app"
            className="flex min-w-0 items-center gap-3"
            aria-label={t("shell.homeLabel")}
          >
            <RbiLogo variant="bankMono" className="h-9 w-auto dark:hidden" />
            <RbiLogo variant="bankYellowInverse" className="hidden h-9 w-auto dark:block" />
            <span className="hidden truncate border-l border-border-subtle pl-3 text-sm font-bold sm:block">
              {t("shell.product")}
            </span>
          </Link>
          <div className="ml-auto flex items-center gap-2">
            <ThemeToggle />
            <LanguageSwitcher className="hidden sm:flex" />
            <Button asChild variant="ghost" size="icon" title={t("shell.profile", { defaultValue: "Moj profil / My profile" })}>
              <Link to="/app/profile"><UserRound className="size-4" /></Link>
            </Button>
            {isAuthenticationConfigured ? <Button
              variant="ghost"
              size="icon"
              title={t("shell.logout", { defaultValue: "Odjava / Sign out" })}
              onClick={() => keycloak.logout({ redirectUri: location.origin })}
            >
              <LogOut className="size-4" />
            </Button> : null}
            <Button
              variant="ghost"
              size="icon"
              className="lg:hidden"
              onClick={() => setOpen((value) => !value)}
              aria-label={t("shell.menu")}
            >
              {open ? <X /> : <Menu />}
            </Button>
          </div>
        </div>
      </header>

      <div className="grid w-full lg:grid-cols-[17rem_minmax(0,1fr)]">
        <aside
          className={`${open ? "flex" : "hidden"} flex-col border-r border-border-subtle bg-surface-subtle p-4 lg:sticky lg:top-16 lg:flex lg:h-[calc(100vh-4rem)] lg:min-h-0`}
        >
          <div className="min-h-0 flex-1 overflow-y-auto">
            {(["work", "administration"] as const).map((area) => (
              <div key={area} className="mb-6">
              <p className="px-3 text-eyebrow text-text-tertiary">{t(`areas.${area}`)}</p>
              <nav className="mt-2 space-y-1" aria-label={t(`areas.${area}`)}>
                {registryResources
                  .filter((item) => item.area === area && hasApplicationAccess(item.accessRole) && (!item.requiresAllAccesses || hasAllApplicationAccesses()))
                  .map((item) => {
                    const active =
                      item.path === "/app"
                        ? pathname === item.path
                        : pathname.startsWith(item.path);
                    return (
                      <Link
                        key={item.key}
                        to={item.path}
                        onClick={() => setOpen(false)}
                        className={`flex min-h-11 items-center gap-3 rounded-sm px-3 py-2 text-sm font-medium transition-colors ${active ? "bg-surface-brand text-text-on-brand" : "text-text-secondary hover:bg-surface-muted hover:text-text-primary"}`}
                      >
                        <item.icon className="size-4 shrink-0" aria-hidden="true" />
                        {t(`resources.${item.key}.title` as never)}
                      </Link>
                    );
                  })}
              </nav>
              </div>
            ))}
          </div>
          <nav className="mt-auto shrink-0 border-t border-border-subtle bg-surface-subtle pt-4" aria-label={t("resources.guide.title", { defaultValue: "Korisnički vodič" })}>
            <Link
              to="/app/guide"
              onClick={() => setOpen(false)}
              className={`flex min-h-11 items-center gap-3 rounded-sm px-3 py-2 text-sm font-medium transition-colors ${pathname.startsWith("/app/guide") ? "bg-surface-brand text-text-on-brand" : "text-text-secondary hover:bg-surface-muted hover:text-text-primary"}`}
            >
              <BookOpenCheck className="size-4 shrink-0" aria-hidden="true" />
              {t("resources.guide.title", { defaultValue: "Korisnički vodič" })}
            </Link>
          </nav>
        </aside>
        <main id="main-content" className="min-w-0 px-4 py-8 lg:px-10 lg:py-10">
          {(() => {
            const current = registryResources.find((item) => item.path !== "/app" && pathname.startsWith(item.path));
            return hasApplicationAccess(current?.accessRole) && (!current?.requiresAllAccesses || hasAllApplicationAccesses());
          })()
            ? <Outlet />
            : <AccessDenied />}
        </main>
      </div>
    </div>
  );
}

function AccessDenied() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  return (
    <section className="mx-auto max-w-xl rounded-sm border border-border-subtle bg-surface-default p-8 text-center shadow-sm">
      <UserRound className="mx-auto size-10 text-text-tertiary" />
      <h1 className="mt-4 text-2xl font-bold">{bs ? "Nemate pristup ovom području" : "You do not have access to this area"}</h1>
      <p className="mt-2 text-text-secondary">{bs ? "Zatražite odgovarajući funkcionalni pristup od osobe koja upravlja korisnicima." : "Ask the user administrator for the corresponding functional access."}</p>
      <Button asChild className="mt-6"><Link to="/app">{bs ? "Nazad na početnu" : "Back to home"}</Link></Button>
    </section>
  );
}

import * as React from "react";
import { useTranslation } from "react-i18next";

import { Dock, dockItemClasses } from "@/components/layout/dock";
import { Container } from "@/components/layout/primitives";

import { Display, Eyebrow, Text } from "@/components/ui/typography";
import { cn } from "@/lib/utils";

/**
 * Shared page furniture for the design-system catalog pages: the page header,
 * section wrappers, and the sidebar navigation contract.
 *
 * Each documented section registers an `id` so the sidebar can link to it and
 * highlight the section currently in view. Headings step down by exactly one
 * level from the page `h1`, which keeps the document outline usable for screen
 * reader navigation.
 */

export interface CatalogPageHeaderProps {
  eyebrow: string;
  title: string;
  description: string;
  children?: React.ReactNode;
}

export function CatalogPageHeader({ eyebrow, title, description, children }: CatalogPageHeaderProps) {
  return (
    <header data-surface="subtle" className="border-b border-border-subtle">
      <Container width="wide" className="py-12 lg:py-20">
        <Eyebrow>{eyebrow}</Eyebrow>
        <Display as="h1" size="md" className="mt-3 max-w-3xl">
          {title}
        </Display>
        <Text size="lg" tone="secondary" className="mt-4 max-w-prose">
          {description}
        </Text>
        {children ? <div className="mt-8">{children}</div> : null}
      </Container>
    </header>
  );
}

export interface CatalogSectionProps {
  id: string;
  title: string;
  description?: string;
  children: React.ReactNode;
}

/** A documented section of the catalog. Rendered as `h2` under the page `h1`. */
export function CatalogSection({ id, title, description, children }: CatalogSectionProps) {
  return (
    <section
      id={id}
      aria-labelledby={`${id}-heading`}
      className="scroll-mt-32 border-b border-border-subtle py-12 first:pt-0 lg:py-16"
    >
      <h2
        id={`${id}-heading`}
        className="brand-rule text-xl font-bold tracking-tight text-text-primary sm:text-2xl"
      >
        {title}
      </h2>
      {description ? (
        <Text tone="secondary" className="mt-3 max-w-prose">
          {description}
        </Text>
      ) : null}
      <div className="mt-8">{children}</div>
    </section>
  );
}

export interface CatalogSubsectionProps {
  title: string;
  description?: string;
  children: React.ReactNode;
}

export function CatalogSubsection({ title, description, children }: CatalogSubsectionProps) {
  return (
    <div className="mt-12 first:mt-0">
      <h3 className="text-lg font-bold text-text-primary">{title}</h3>
      {description ? (
        <Text size="sm" tone="secondary" className="mt-1.5 max-w-prose">
          {description}
        </Text>
      ) : null}
      <div className="mt-4">{children}</div>
    </div>
  );
}

export interface CatalogNavItem {
  id: string;
  label: string;
}

/**
 * Tracks which registered section is currently in view, so the sidebar can
 * reflect reading position. Uses a viewport-band root margin rather than the
 * default, so the active item changes when a heading reaches the upper third of
 * the screen instead of the very bottom.
 */
function useActiveSection(ids: readonly string[]) {
  const [active, setActive] = React.useState<string | undefined>(ids[0]);

  React.useEffect(() => {
    const elements = ids
      .map((id) => document.getElementById(id))
      .filter((element): element is HTMLElement => element !== null);
    if (elements.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0];
        if (visible) setActive(visible.target.id);
      },
      { rootMargin: "-96px 0px -60% 0px", threshold: 0 },
    );

    elements.forEach((element) => observer.observe(element));
    return () => observer.disconnect();
  }, [ids]);

  return active;
}

/**
 * Catalog sidebar navigation.
 *
 * Desktop: a sticky rail with a yellow indicator on the section in view.
 * Mobile and tablet: the same list collapses into a horizontally scrollable
 * chip row pinned under the header, which keeps section switching one tap away
 * instead of forcing a long scroll.
 */
export function CatalogNav({ items, label }: { items: readonly CatalogNavItem[]; label: string }) {
  const { t } = useTranslation("components");
  const ids = React.useMemo(() => items.map((item) => item.id), [items]);
  const active = useActiveSection(ids);

  return (
    <>
      <div className="sticky top-16 z-20 -mx-4 py-3 lg:hidden">
        <Dock label={label}>
          {items.map((item) => (
            <li key={item.id}>
              <a
                href={`#${item.id}`}
                aria-current={active === item.id ? "true" : undefined}
                className={dockItemClasses(active === item.id)}
              >
                {item.label}
              </a>
            </li>
          ))}
        </Dock>
      </div>


      <nav aria-label={label} className="sticky top-24 hidden max-h-[calc(100vh-8rem)] overflow-y-auto lg:block">
        <p className="text-eyebrow mb-3 text-text-tertiary">{t("nav.onThisPage")}</p>
        <ul className="border-l border-border-subtle">
          {items.map((item) => (
            <li key={item.id}>
              <a
                href={`#${item.id}`}
                aria-current={active === item.id ? "true" : undefined}
                className={cn(
                  "-ml-px flex min-h-16 items-center border-l-3 py-1.5 pl-3 text-sm",
                  "transition-colors duration-150 ease-standard",
                  active === item.id
                    ? "border-l-border-brand font-bold text-text-primary"
                    : "border-l-transparent text-text-secondary hover:border-l-border-default hover:text-text-primary",
                )}
              >
                {item.label}
              </a>
            </li>
          ))}
        </ul>
      </nav>
    </>
  );
}

/** Two-column catalog body: sticky sidebar on the left, documented sections right. */
export function CatalogLayout({ nav, children }: { nav: React.ReactNode; children: React.ReactNode }) {
  return (
    <Container width="wide" className="pb-16 lg:py-16">
      <div className="grid gap-8 lg:grid-cols-[13rem_minmax(0,1fr)] lg:gap-12 xl:gap-16">
        <div className="min-w-0">{nav}</div>
        <div className="min-w-0 pt-8 lg:pt-0 [&>section:last-child]:border-b-0">{children}</div>
      </div>
    </Container>
  );
}

import type { ReactNode } from "react";

import { Display, Eyebrow, Text } from "@/components/ui/typography";

export function HeroSection({ eyebrow, title, lead, actions, footer }: { readonly eyebrow: string; readonly title: string; readonly lead: string; readonly actions?: ReactNode; readonly footer?: ReactNode }) {
  return (
    <section data-surface="brand" className="relative overflow-hidden rounded-sm bg-surface-brand px-6 py-10 sm:px-10 lg:py-14">
      <div aria-hidden="true" className="brand-hatch pointer-events-none absolute -right-16 -top-20 hidden size-72 rotate-12 text-text-primary/15 lg:block" />
      <div className="relative max-w-4xl">
        <Eyebrow>{eyebrow}</Eyebrow>
        <Display as="h1" size="xl" className="mt-4 max-w-3xl">{title}</Display>
        <Text size="xl" className="mt-6 max-w-prose text-text-primary">{lead}</Text>
        {actions ? <div className="mt-9 flex flex-wrap gap-3">{actions}</div> : null}
        {footer ? <div className="mt-8 flex flex-wrap gap-x-6 gap-y-2 border-t border-text-primary/20 pt-5 text-sm font-medium text-text-primary">{footer}</div> : null}
      </div>
    </section>
  );
}

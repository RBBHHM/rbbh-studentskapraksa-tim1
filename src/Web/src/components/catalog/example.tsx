import * as React from "react";
import { Check, Copy } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { Text } from "@/components/ui/typography";
import { cn } from "@/lib/utils";

/**
 * A live component example with its source.
 *
 * The preview is the real component — never a screenshot — so what is
 * documented is always what ships. The source panel is collapsible and the
 * copy button reports its result through a live region rather than a toast, so
 * confirmation is available to screen reader users without stealing focus.
 */
export interface ExampleProps {
  title: string;
  description?: string;
  /** Source snippet shown in the collapsible panel. */
  code?: string;
  /** Set on the preview wrapper to switch the surface context. */
  surface?: "default" | "subtle" | "inverse" | "brand" | "corporate";
  className?: string;
  children: React.ReactNode;
}

export function Example({ title, description, code, surface = "subtle", className, children }: ExampleProps) {
  const { t } = useTranslation("components");
  const [copied, setCopied] = React.useState(false);
  const [showCode, setShowCode] = React.useState(false);
  const timeout = React.useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  React.useEffect(() => () => clearTimeout(timeout.current), []);

  async function copy() {
    if (!code) return;
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      clearTimeout(timeout.current);
      timeout.current = setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  }

  return (
    <figure className="m-0 rounded-sm border border-border-default">
      <figcaption className="flex flex-wrap items-start justify-between gap-3 border-b border-border-subtle px-4 py-3">
        <div className="min-w-0">
          <h4 className="text-sm font-bold text-text-primary">{title}</h4>
          {description ? (
            <Text size="sm" tone="secondary" className="mt-1 max-w-prose">
              {description}
            </Text>
          ) : null}
        </div>
        {code ? (
          <div className="flex shrink-0 items-center gap-1">
            <Button variant="ghost" size="sm" aria-expanded={showCode} onClick={() => setShowCode((v) => !v)}>
              {showCode ? t("example.hideCode") : t("example.showCode")}
            </Button>
            <Button variant="ghost" size="icon-sm" onClick={copy} aria-label={t("example.copyAriaLabel")}>
              {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
            </Button>
          </div>
        ) : null}
      </figcaption>

      <div
        data-surface={surface === "default" ? undefined : surface}
        className={cn("flex flex-wrap items-center gap-4 p-6", className)}
      >
        {children}
      </div>

      <p aria-live="polite" className="sr-only">
        {copied ? t("example.codeCopied") : ""}
      </p>

      {code && showCode ? (
        <pre className="overflow-x-auto border-t border-border-subtle bg-surface-sunken p-4 text-xs leading-relaxed">
          <code className="font-mono text-text-primary">{code}</code>
        </pre>
      ) : null}
    </figure>
  );
}

/**
 * Documents the keyboard and assistive-technology contract of a component.
 * Present on every component in the catalog — an interaction that is not
 * documented here is not considered done.
 */
export function A11yNotes({ items }: { items: readonly { key: string; behaviour: string }[] }) {
  const { t } = useTranslation("components");
  return (
    <div className="mt-4 rounded-sm border border-border-subtle bg-surface-subtle p-4">
      <h4 className="text-eyebrow mb-3 text-text-secondary">{t("a11yNotes.heading")}</h4>
      <dl className="grid gap-x-8 gap-y-2 sm:grid-cols-[auto_minmax(0,1fr)]">
        {items.map((item) => (
          <React.Fragment key={item.key}>
            <dt className="text-xs">
              <kbd className="rounded-xs border border-border-default bg-surface px-1.5 py-0.5 font-mono text-2xs text-text-primary">
                {item.key}
              </kbd>
            </dt>
            <dd className="mb-1 text-xs text-text-secondary sm:mb-0">{item.behaviour}</dd>
          </React.Fragment>
        ))}
      </dl>
    </div>
  );
}

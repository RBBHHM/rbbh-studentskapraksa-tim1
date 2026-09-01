import * as React from "react";
import { Check, Copy, FileCode2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Text } from "@/components/ui/typography";
import { cn } from "@/lib/utils";

/**
 * A multi-file source panel with a clipboard action.
 *
 * The sources handed to this component are imported as raw text by the Webpack configuration,
 * so what a reader copies is byte-identical to what ships — there is no second,
 * hand-maintained copy of any snippet in the repository.
 */

export interface CodePanelFile {
  /** Path shown in the tab, relative to the repository root. */
  readonly name: string;
  readonly code: string;
  readonly description?: string;
}

export interface CodePanelProps {
  files: readonly CodePanelFile[];
  className?: string;
  /** Collapsed by default: a full page source is long. */
  defaultOpen?: boolean;
}

export function CodePanel({ files, className, defaultOpen = false }: CodePanelProps) {
  const [activeName, setActiveName] = React.useState(files[0]?.name ?? "");
  const [open, setOpen] = React.useState(defaultOpen);
  const [copied, setCopied] = React.useState(false);
  const timeout = React.useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  React.useEffect(() => () => clearTimeout(timeout.current), []);

  const active = files.find((file) => file.name === activeName) ?? files[0];

  async function copy() {
    if (!active) return;
    try {
      await navigator.clipboard.writeText(active.code);
      setCopied(true);
      clearTimeout(timeout.current);
      timeout.current = setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  }

  if (!active) return null;

  const lines = active.code.split("\n").length;

  return (
    <div className={cn("overflow-hidden rounded-sm border border-border-default", className)}>
      <div className="glass-strong flex flex-wrap items-center justify-between gap-3 border-b border-border-subtle px-4 py-3">
        <div className="flex min-w-0 flex-wrap items-center gap-1.5">
          {files.map((file) => (
            <button
              key={file.name}
              type="button"
              aria-pressed={file.name === active.name}
              onClick={() => setActiveName(file.name)}
              className={cn(
                "inline-flex h-8 items-center gap-1.5 rounded-full border px-3 font-mono text-xs",
                "transition-colors duration-150 ease-standard",
                file.name === active.name
                  ? "border-border-strong bg-surface-brand font-bold text-text-on-brand"
                  : "border-border-subtle text-text-secondary hover:text-text-primary",
              )}
            >
              <FileCode2 aria-hidden="true" className="size-3.5" />
              {file.name.split("/").pop()}
            </button>
          ))}
        </div>
        <div className="flex shrink-0 items-center gap-1">
          <Button variant="ghost" size="sm" aria-expanded={open} onClick={() => setOpen((value) => !value)}>
            {open ? "Hide source" : "Show source"}
          </Button>
          <Button variant="secondary" size="sm" onClick={copy}>
            {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
            {copied ? "Copied" : "Copy file"}
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-baseline justify-between gap-2 border-b border-border-subtle px-4 py-2">
        <p className="font-mono text-xs text-text-secondary">{active.name}</p>
        <p className="font-mono text-2xs text-text-tertiary tabular-nums">{lines} lines</p>
      </div>

      {active.description ? (
        <Text size="sm" tone="secondary" className="px-4 py-3">
          {active.description}
        </Text>
      ) : null}

      <p aria-live="polite" className="sr-only">
        {copied ? `${active.name} copied to clipboard` : ""}
      </p>

      {open ? (
        <pre className="max-h-[32rem] overflow-auto bg-surface-sunken p-4 text-xs leading-relaxed">
          <code className="font-mono text-text-primary">{active.code}</code>
        </pre>
      ) : null}
    </div>
  );
}

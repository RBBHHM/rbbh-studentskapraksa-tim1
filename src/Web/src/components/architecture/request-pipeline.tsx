import { ArrowRight } from "lucide-react";

import { useSafeTranslation } from "@/localization";
import { pipelineNodes } from "@/lib/architecture/structure";

/**
 * Request pipeline — one read, drawn end to end.
 *
 * Purely presentational: the hops come from src/lib/architecture/structure.ts
 * and every label is resolved from the "architecture" namespace.
 */
export function RequestPipeline() {
  const { text } = useSafeTranslation("architecture");

  return (
    <ol className="flex flex-col gap-3 lg:flex-row lg:items-stretch">
      {pipelineNodes.map((node, index) => (
        <li key={node.key} className="flex min-w-0 flex-1 items-center gap-3">
          <div
            data-surface={node.side === "service" ? "inverse" : undefined}
            className="glass min-w-0 flex-1 rounded-sm border border-t-3 border-border-subtle border-t-border-brand p-4"
          >
            <div className="flex items-center gap-2">
              <node.icon aria-hidden="true" className="size-4 shrink-0" />
              <span className="font-mono text-2xs text-text-tertiary tabular-nums">
                0{index + 1}
              </span>
            </div>
            <p className="mt-2 text-sm font-bold text-text-primary">
              {text(`pipeline.nodes.${node.key}.title`)}
            </p>
            <p className="mt-1 text-sm text-text-secondary">
              {text(`pipeline.nodes.${node.key}.body`)}
            </p>
            <code className="mt-3 block truncate font-mono text-2xs text-text-tertiary">
              {node.file}
            </code>
          </div>
          {index < pipelineNodes.length - 1 ? (
            <ArrowRight
              aria-hidden="true"
              className="size-4 shrink-0 rotate-90 text-text-tertiary lg:rotate-0"
            />
          ) : null}
        </li>
      ))}
    </ol>
  );
}

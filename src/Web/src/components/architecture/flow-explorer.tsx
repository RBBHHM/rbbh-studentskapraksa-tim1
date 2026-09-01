import * as React from "react";
import { ArrowRight, FileCode2 } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Badge } from "@/components/ui/badge";
import { flows, zoneBorders, type Flow, type FlowStep } from "@/lib/architecture/flows";
import { cn } from "@/lib/utils";
import { useSafeTranslation } from "@/localization";

/**
 * Interactive architecture flow explorer.
 *
 * The rail is a listbox of steps; selecting one opens the detail panel below it —
 * which file owns the step, what it is responsible for, what it hands to the next
 * step and the call that does it. Arrow keys move along the rail, so the diagram
 * is navigable without a pointer.
 *
 * Structural data comes from src/lib/architecture/flows.ts; copy comes from the
 * "architecture" namespace, keyed by flow and step id.
 */

function useStepSelection(flow: Flow) {
  const [selectedId, setSelectedId] = React.useState<string>(flow.steps[0]!.id);
  const index = Math.max(
    0,
    flow.steps.findIndex((step) => step.id === selectedId),
  );

  const move = React.useCallback(
    (delta: number) => {
      const next = flow.steps[(index + delta + flow.steps.length) % flow.steps.length]!;
      setSelectedId(next.id);
    },
    [flow.steps, index],
  );

  return { selected: flow.steps[index]!, selectedId, setSelectedId, move };
}

function StepDetail({ flow, step }: { flow: Flow; step: FlowStep }) {
  const { t } = useTranslation("architecture");
  const { list } = useSafeTranslation("architecture");
  const responsibilities = list(`flows.${flow.id}.steps.${step.id}.responsibilities`);

  return (
    <div className="glass-strong mt-4 rounded-sm border p-5 sm:p-6">
      <div className="flex flex-wrap items-center gap-3">
        <Badge variant="outline">{t(`flows.zones.${step.zone}` as never)}</Badge>
        <p className="text-base font-bold text-text-primary">{t(`flows.${flow.id}.steps.${step.id}.label` as never)}</p>
      </div>

      <p className="mt-2 flex items-center gap-2 font-mono text-2xs text-text-tertiary">
        <FileCode2 aria-hidden="true" className="size-3.5" />
        {step.file}
      </p>

      <div className="mt-5 grid gap-6 lg:grid-cols-2">
        <div>
          <p className="text-eyebrow text-text-tertiary">{t("flows.labels.responsibleFor")}</p>
          <ul className="mt-2 space-y-1.5">
            {responsibilities.map((item) => (
              <li key={item} className="flex gap-2 text-sm text-text-secondary">
                <span aria-hidden="true" className="mt-2 size-1.5 shrink-0 rounded-full bg-surface-brand" />
                {item}
              </li>
            ))}
          </ul>

          <p className="text-eyebrow mt-5 text-text-tertiary">{t("flows.labels.handsOff")}</p>
          <p className="mt-1.5 font-mono text-xs text-text-primary">
            {t(`flows.${flow.id}.steps.${step.id}.handsOff` as never)}
          </p>
        </div>

        <div className="min-w-0">
          <p className="text-eyebrow text-text-tertiary">{t("flows.labels.inCode")}</p>
          <pre className="mt-2 overflow-x-auto rounded-sm border border-border-subtle bg-surface-subtle p-4 font-mono text-2xs leading-relaxed text-text-primary">
            <code>{step.snippet}</code>
          </pre>
        </div>
      </div>
    </div>
  );
}

function FlowRail({ flow }: { flow: Flow }) {
  const { t } = useTranslation("architecture");
  const { selected, selectedId, setSelectedId, move } = useStepSelection(flow);
  const title = t(`flows.${flow.id}.title` as never);

  return (
    <div>
      <h3 className="flex items-center gap-2 text-sm font-bold text-text-primary">
        <flow.icon aria-hidden="true" className="size-4" />
        {title}
      </h3>
      <p className="mt-2 max-w-prose text-sm text-text-secondary">{t(`flows.${flow.id}.summary` as never)}</p>

      <ol
        className="mt-4 grid gap-3 lg:grid-cols-[repeat(4,minmax(0,1fr))]"
        role="tablist"
        aria-label={t("flows.labels.stepsAriaLabel", { title })}
        onKeyDown={(event) => {
          if (event.key === "ArrowRight" || event.key === "ArrowDown") {
            event.preventDefault();
            move(1);
          }
          if (event.key === "ArrowLeft" || event.key === "ArrowUp") {
            event.preventDefault();
            move(-1);
          }
        }}
      >
        {flow.steps.map((step, index) => {
          const active = step.id === selectedId;
          return (
            <li key={step.id} className="relative">
              <button
                type="button"
                role="tab"
                id={`${step.id}-tab`}
                aria-selected={active}
                aria-controls={`${flow.id}-detail`}
                tabIndex={active ? 0 : -1}
                onClick={() => setSelectedId(step.id)}
                className={cn(
                  "glass h-full w-full rounded-sm border border-t-3 p-4 text-left",
                  "transition-[border-color,transform,box-shadow] duration-200 ease-standard",
                  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[color:var(--focus-ring-color)]",
                  zoneBorders[step.zone],
                  active
                    ? "border-border-strong shadow-md lg:-translate-y-0.5"
                    : "border-border-subtle hover:border-border-default",
                )}
              >
                <span className="flex items-center justify-between gap-2">
                  <Badge variant="outline">{t(`flows.zones.${step.zone}` as never)}</Badge>
                  <span className="font-mono text-2xs text-text-tertiary tabular-nums">0{index + 1}</span>
                </span>
                <span className="mt-3 block text-sm font-bold text-text-primary">
                  {t(`flows.${flow.id}.steps.${step.id}.label` as never)}
                </span>
                <span className="mt-1 block text-sm text-text-secondary">
                  {t(`flows.${flow.id}.steps.${step.id}.detail` as never)}
                </span>
              </button>
              {index < flow.steps.length - 1 ? (
                <ArrowRight
                  aria-hidden="true"
                  className="absolute top-1/2 -right-2.5 hidden size-4 -translate-y-1/2 text-text-tertiary lg:block"
                />
              ) : null}
            </li>
          );
        })}
      </ol>

      <div id={`${flow.id}-detail`} role="tabpanel" aria-labelledby={`${selected.id}-tab`}>
        <StepDetail flow={flow} step={selected} />
      </div>
    </div>
  );
}

export function FlowExplorer() {
  return (
    <div className="space-y-12">
      {flows.map((flow) => (
        <FlowRail key={flow.id} flow={flow} />
      ))}
    </div>
  );
}

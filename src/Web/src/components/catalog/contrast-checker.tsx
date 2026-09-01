import * as React from "react";
import { Check, X } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
} from "@/components/ui/select";
import { Text } from "@/components/ui/typography";
import {
  defaultContrastPair,
  paletteByToken,
  paletteGroups,
  type PaletteOption,
} from "@/design-system/foundations/palette";
import { contrastRequirements, evaluateContrast, type ContrastVerdict } from "@/lib/color/contrast";
import { cn } from "@/lib/utils";

/**
 * Contrast checker.
 *
 * Two predefined palette colours in, a WCAG 2.2 verdict out. The palette comes
 * from the token catalog, so only colours that actually exist in the brand can
 * be checked — this is a governance tool, not a generic colour picker.
 */

const verdictClasses: Record<ContrastVerdict, string> = {
  aaa: "border-feedback-success-border bg-feedback-success-bg text-feedback-success",
  aa: "border-feedback-success-border bg-feedback-success-bg text-feedback-success",
  fail: "border-feedback-danger-border bg-feedback-danger-bg text-feedback-danger",
};

function ColorSwatch({ hex }: { readonly hex: string }) {
  return (
    <span
      aria-hidden="true"
      data-color-preview={hex}
      style={{
        backgroundColor: hex,
        border: "1px solid color-mix(in srgb, var(--border-default) 72%, transparent)",
        borderRadius: "var(--radii-sm)",
        boxShadow:
          "0 1px 2px color-mix(in srgb, var(--text-primary) 14%, transparent), inset 0 0 0 1px color-mix(in srgb, var(--surface-default) 55%, transparent)",
        display: "inline-block",
        flex: "0 0 24px",
        height: "24px",
        minHeight: "24px",
        minWidth: "24px",
        width: "24px",
      }}
    />
  );
}

function PaletteSelect({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (token: string) => void;
}) {
  const selectedOption = paletteByToken.get(value);

  return (
    <div className="min-w-0">
      <Label htmlFor={id}>{label}</Label>
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger
          id={id}
          className="mt-1.5"
          aria-label={`${label}: ${selectedOption?.label ?? value}`}
        >
          {selectedOption ? (
            <span
              className="min-w-0 flex-1 items-center gap-2 overflow-hidden pr-2"
              style={{ display: "flex" }}
            >
              <ColorSwatch hex={selectedOption.hex} />
              <span className="min-w-0 truncate">{selectedOption.label}</span>
              <span className="ml-auto shrink-0 font-mono text-2xs text-text-tertiary">
                {selectedOption.hex}
              </span>
            </span>
          ) : (
            <span>{value}</span>
          )}
        </SelectTrigger>

        <SelectContent className="max-h-80">
          {paletteGroups.map((group) => (
            <SelectGroup key={group.group}>
              <SelectLabel>{group.group}</SelectLabel>
              {group.options.map((option) => (
                <SelectItem key={option.token} value={option.token}>
                  <span className="flex w-full min-w-0 items-center gap-2">
                    <ColorSwatch hex={option.hex} />
                    <span className="min-w-0 truncate">{option.label}</span>
                    <span className="ml-auto pl-2 font-mono text-2xs text-text-tertiary">
                      {option.hex}
                    </span>
                  </span>
                </SelectItem>
              ))}
            </SelectGroup>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}

function describe(option: PaletteOption | undefined, unknownLabel: string) {
  return option ? `${option.label} (${option.hex})` : unknownLabel;
}

export function ContrastChecker() {
  const { t } = useTranslation("foundations");
  const [foreground, setForeground] = React.useState<string>(defaultContrastPair.foreground);
  const [background, setBackground] = React.useState<string>(defaultContrastPair.background);

  const verdictLabels: Record<ContrastVerdict, string> = {
    aaa: t("contrastChecker.verdicts.aaa"),
    aa: t("contrastChecker.verdicts.aa"),
    fail: t("contrastChecker.verdicts.fail"),
  };

  const foregroundOption = paletteByToken.get(foreground);
  const backgroundOption = paletteByToken.get(background);
  const result = evaluateContrast(
    foregroundOption?.hex ?? "#000000",
    backgroundOption?.hex ?? "#FFFFFF",
  );
  const foregroundDescription = describe(foregroundOption, t("contrastChecker.unknownColor"));
  const backgroundDescription = describe(backgroundOption, t("contrastChecker.unknownColor"));

  function swap() {
    setForeground(background);
    setBackground(foreground);
  }

  return (
    <div className="rounded-sm border border-border-default">
      <div className="grid gap-4 border-b border-border-subtle p-4 sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto] sm:items-end">
        <PaletteSelect
          id="contrast-foreground"
          label={t("contrastChecker.foregroundLabel")}
          value={foreground}
          onChange={setForeground}
        />
        <PaletteSelect
          id="contrast-background"
          label={t("contrastChecker.backgroundLabel")}
          value={background}
          onChange={setBackground}
        />
        <Button variant="secondary" onClick={swap}>
          {t("contrastChecker.swap")}
        </Button>
      </div>

      <div className="grid gap-0 md:grid-cols-[minmax(0,20rem)_minmax(0,1fr)]">
        <div
          className="flex flex-col justify-center gap-2 border-b border-border-subtle p-6 md:border-r md:border-b-0"
          style={{ backgroundColor: backgroundOption?.hex, color: foregroundOption?.hex }}
        >
          <p className="font-brand text-2xl font-black" style={{ lineHeight: 1.1 }}>
            {t("contrastChecker.sampleHeading")}
          </p>
          <p className="text-sm">{t("contrastChecker.sampleBody")}</p>
          <p className="font-mono text-xs">
            {t("contrastChecker.onLabel", {
              foreground: foregroundDescription,
              background: backgroundDescription,
            })}
          </p>
        </div>

        <div className="p-6">
          <div className="flex flex-wrap items-baseline gap-3">
            <p className="font-brand text-4xl font-black tabular-nums text-text-primary">
              {result.display}
            </p>
            <span
              className={cn(
                "inline-flex items-center gap-1.5 rounded-pill border px-2.5 py-1 text-xs font-bold",
                result.passesAllAa
                  ? "border-feedback-success-border bg-feedback-success-bg text-feedback-success"
                  : "border-feedback-warning-border bg-feedback-warning-bg text-feedback-warning",
              )}
            >
              {result.passesAllAa
                ? t("contrastChecker.usableEverywhere")
                : t("contrastChecker.restrictedUse")}
            </span>
          </div>

          <Text size="sm" tone="secondary" className="mt-2">
            {result.passesAllAa
              ? t("contrastChecker.passSummary")
              : t("contrastChecker.failSummary")}
          </Text>

          <dl className="mt-5 space-y-3">
            {contrastRequirements.map((requirement) => {
              const verdict = result.verdicts[requirement.id];
              return (
                <div
                  key={requirement.id}
                  className="grid grid-cols-[minmax(0,1fr)_auto] items-start gap-3 border-t border-border-subtle pt-3 first:border-t-0 first:pt-0"
                >
                  <div className="min-w-0">
                    <dt className="text-sm font-bold text-text-primary">
                      {requirement.label}
                      <span className="ml-2 font-mono text-xs font-normal text-text-tertiary">
                        {`≥ ${requirement.aa}:1`}
                        {requirement.aaa ? ` · AAA ≥ ${requirement.aaa}:1` : ""}
                      </span>
                    </dt>
                    <dd className="mt-0.5 text-xs text-text-secondary">
                      {requirement.description}
                    </dd>
                  </div>
                  <dd
                    className={cn(
                      "inline-flex shrink-0 items-center gap-1.5 rounded-pill border px-2.5 py-1 text-xs font-bold",
                      verdictClasses[verdict],
                    )}
                  >
                    {verdict === "fail" ? (
                      <X aria-hidden="true" className="size-3.5" />
                    ) : (
                      <Check aria-hidden="true" className="size-3.5" />
                    )}
                    {verdictLabels[verdict]}
                  </dd>
                </div>
              );
            })}
          </dl>

          <p aria-live="polite" className="sr-only">
            {t("contrastChecker.liveRegion", {
              foreground: foregroundDescription,
              background: backgroundDescription,
              result: result.display,
              status: result.passesAllAa
                ? t("contrastChecker.passing")
                : t("contrastChecker.failing"),
            })}
          </p>
        </div>
      </div>
    </div>
  );
}

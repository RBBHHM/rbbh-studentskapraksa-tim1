import * as React from "react";
import { Check, Copy, Plus, RotateCcw, Trash2 } from "lucide-react";
import { useTranslation } from "react-i18next";

import { BentoCard, BentoCardBody, BentoGrid } from "@/components/ui/bento-grid";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Text } from "@/components/ui/typography";
import {
  bentoPresets,
  bentoRowSpanOptions,
  bentoSpanOptions,
  bentoToneOptions,
  serializeBento,
  type BentoTile,
} from "@/design-system/foundations/bento-presets";
import { cn } from "@/lib/utils";

/**
 * Bento layout builder.
 *
 * Pick a blueprint, adjust each tile's width, height and tone, then copy the
 * JSX. Preview and code are produced from the same tile array, so what is
 * copied is exactly what is on screen.
 */

type Gap = "tight" | "default" | "loose";
type Density = "uniform" | "tall";

const gapOptions: readonly Gap[] = ["tight", "default", "loose"];
const densityOptions: readonly Density[] = ["uniform", "tall"];

function nextInCycle<T>(options: readonly T[], current: T): T {
  const index = options.indexOf(current);
  return options[(index + 1) % options.length]!;
}

/**
 * Wireframe preview of a blueprint: each tile drawn at its real span and row
 * height, so the shape of the layout is readable before any copy is.
 */
function BlueprintThumbnail({ tiles }: { tiles: readonly BentoTile[] }) {
  return (
    <span
      aria-hidden="true"
      className="mt-3 grid grid-cols-12 gap-1 rounded-sm bg-surface-sunken p-1.5"
      style={{ gridAutoRows: "0.75rem" }}
    >
      {tiles.map((tile) => (
        <span
          key={tile.id}
          className={cn(
            "rounded-[2px]",
            tile.tone === "brand"
              ? "bg-surface-brand"
              : tile.tone === "inverse"
                ? "bg-surface-inverse"
                : tile.tone === "corporate"
                  ? "bg-surface-corporate"
                  : tile.tone === "subtle"
                    ? "bg-border-default"
                    : "bg-border-subtle",
          )}
          style={{ gridColumn: `span ${tile.span}`, gridRow: `span ${tile.rowSpan}` }}
        />
      ))}
    </span>
  );
}

function newTile(index: number, t: (key: any) => any): BentoTile {

  return {
    id: `tile-${index}`,
    eyebrow: t("bentoBuilder.newTile.eyebrow"),
    title: t("bentoBuilder.newTile.title").replace("{index}", String(index)),
    body: t("bentoBuilder.newTile.body"),
    span: 4,
    rowSpan: 1,
    tone: "default",
    accent: true,
  };
}

export function BentoBuilder() {
  const { t } = useTranslation("components");
  const [presetId, setPresetId] = React.useState(bentoPresets[0]!.id);
  const preset = bentoPresets.find((entry) => entry.id === presetId) ?? bentoPresets[0]!;

  const [tiles, setTiles] = React.useState<readonly BentoTile[]>(preset.tiles);
  const [gap, setGap] = React.useState<Gap>("default");
  const [density, setDensity] = React.useState<Density>("uniform");
  const [showCode, setShowCode] = React.useState(false);
  const [copied, setCopied] = React.useState(false);

  function selectPreset(id: string) {
    setPresetId(id);
    setTiles(bentoPresets.find((entry) => entry.id === id)?.tiles ?? []);
  }

  function patchTile(id: string, patch: Partial<BentoTile>) {
    setTiles((current) => current.map((tile) => (tile.id === id ? { ...tile, ...patch } : tile)));
  }

  const code = serializeBento(tiles, { gap, density });

  async function copy() {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  }

  return (
    <div className="rounded-sm border border-border-default">
      <fieldset className="border-b border-border-subtle p-4">
        <legend className="text-eyebrow mb-3 text-text-tertiary">{t("bentoBuilder.blueprintLegend")}</legend>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {bentoPresets.map((entry) => (
            <label
              key={entry.id}
              className={cn(
                "flex cursor-pointer flex-col rounded-sm border p-3 transition-colors duration-150 ease-standard",
                entry.id === presetId
                  ? "border-border-strong bg-surface-brand-faint"
                  : "border-border-subtle bg-surface hover:border-border-default",
              )}
            >
              <span className="flex items-center gap-2">
                <input
                  type="radio"
                  name="bento-preset"
                  value={entry.id}
                  checked={entry.id === presetId}
                  onChange={() => selectPreset(entry.id)}
                  className="size-4 accent-[color:var(--surface-brand)]"
                />
                <span className="text-sm font-bold text-text-primary">
                  {t(`bentoBuilder.presets.${entry.messageKey}.label` as never) as string}
                </span>
              </span>
              <BlueprintThumbnail tiles={entry.tiles} />
              <span className="mt-2 text-xs text-text-secondary">
                {t(`bentoBuilder.presets.${entry.messageKey}.description` as never) as string}
              </span>
            </label>
          ))}
        </div>
      </fieldset>


      <div className="grid gap-4 border-b border-border-subtle p-4 sm:grid-cols-[11rem_11rem_minmax(0,1fr)] sm:items-end">
        <div>
          <Label htmlFor="bento-gap">{t("bentoBuilder.gapLabel")}</Label>
          <Select value={gap} onValueChange={(value) => setGap(value as Gap)}>
            <SelectTrigger id="bento-gap" className="mt-1.5">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {gapOptions.map((option) => (
                <SelectItem key={option} value={option}>
                  {t(`bentoBuilder.gap${option.charAt(0).toUpperCase()}${option.slice(1)}` as never) as string}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div>
          <Label htmlFor="bento-density">{t("bentoBuilder.rowHeightLabel")}</Label>
          <Select value={density} onValueChange={(value) => setDensity(value as Density)}>
            <SelectTrigger id="bento-density" className="mt-1.5">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {densityOptions.map((option) => (
                <SelectItem key={option} value={option}>
                  {t(`bentoBuilder.density${option.charAt(0).toUpperCase()}${option.slice(1)}` as never) as string}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-wrap items-center gap-2 sm:justify-end">
          <Button
            variant="ghost"
            onClick={() => setTiles((current) => [...current, newTile(current.length + 1, t)])}
          >
            <Plus aria-hidden="true" /> {t("bentoBuilder.addTile")}
          </Button>
          <Button variant="ghost" onClick={() => selectPreset(presetId)}>
            <RotateCcw aria-hidden="true" /> {t("bentoBuilder.reset")}
          </Button>
          <Button variant="secondary" aria-expanded={showCode} onClick={() => setShowCode((value) => !value)}>
            {showCode ? t("bentoBuilder.hideCode") : t("bentoBuilder.showCode")}
          </Button>
          <Button onClick={copy}>
            {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
            {copied ? t("bentoBuilder.copied") : t("bentoBuilder.copyCode")}
          </Button>
        </div>
      </div>

      <div className="bg-surface-sunken p-4 sm:p-6">
        <BentoGrid gap={gap} density={density}>
          {tiles.map((tile) => (
            <BentoCard key={tile.id} span={tile.span} rowSpan={tile.rowSpan} tone={tile.tone} accent={tile.accent}>
              <div className="mb-3 flex flex-wrap items-center gap-1.5">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => patchTile(tile.id, { span: nextInCycle(bentoSpanOptions, tile.span) })}
                >
                  {t("bentoBuilder.width", { span: tile.span })}
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => patchTile(tile.id, { rowSpan: nextInCycle(bentoRowSpanOptions, tile.rowSpan) })}
                >
                  {t("bentoBuilder.rows", { rowSpan: tile.rowSpan })}
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => patchTile(tile.id, { tone: nextInCycle(bentoToneOptions, tile.tone) })}
                >
                  {tile.tone}
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => patchTile(tile.id, { accent: !tile.accent })}
                >
                  {tile.accent ? t("bentoBuilder.accentOn") : t("bentoBuilder.accentOff")}
                </Button>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={t("bentoBuilder.removeTile", { title: tile.title }) as string}
                  onClick={() => setTiles((current) => current.filter((entry) => entry.id !== tile.id))}
                >
                  <Trash2 aria-hidden="true" />
                </Button>
              </div>
              <BentoCardBody eyebrow={tile.eyebrow} title={tile.title} body={tile.body} />
            </BentoCard>
          ))}
        </BentoGrid>
      </div>

      <p aria-live="polite" className="sr-only">
        {copied ? t("bentoBuilder.codeCopiedStatus") : ""}
      </p>

      {showCode ? (
        <pre className="max-h-96 overflow-auto border-t border-border-subtle bg-surface-sunken p-4 text-xs leading-relaxed">
          <code className="font-mono text-text-primary">{code}</code>
        </pre>
      ) : null}

      {tiles.length === 0 ? (
        <div className="border-t border-border-subtle p-6 text-center">
          <Text size="sm" tone="secondary">
            {t("bentoBuilder.emptyState")}
          </Text>
        </div>
      ) : null}
    </div>
  );
}

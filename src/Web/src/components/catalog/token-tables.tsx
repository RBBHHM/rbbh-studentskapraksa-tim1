import { colorTokenGroups, type ColorTokenGroup, type ScaleEntry } from "@/design-system/tokens";

/**
 * Token catalog tables.
 *
 * Every swatch and every sample is rendered from the live CSS custom property,
 * never from a hard-coded value, so this page is a mirror of the implementation
 * rather than a second source of truth that can drift.
 */

function isDarkToken(token: string) {
  return (
    token.includes("off-black") ||
    token.includes("mono-black") ||
    token.includes("inverse") ||
    token.includes("green-400") ||
    token.includes("green-500") ||
    token.includes("grey-600") ||
    token.includes("grey-700") ||
    token.includes("grey-800") ||
    token.includes("grey-900") ||
    token.includes("purple") ||
    token.includes("coral-500")
  );
}

export function ColorTokenTable({ group }: { group: ColorTokenGroup }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[44rem] border-collapse text-left text-sm">
        <caption className="sr-only">{`${group.title} tokens: swatch, token name, value and usage`}</caption>
        <thead>
          <tr className="border-b border-border-default">
            <th scope="col" className="w-24 py-2 pr-4 font-medium text-text-secondary">
              Swatch
            </th>
            <th scope="col" className="py-2 pr-4 font-medium text-text-secondary">
              Token
            </th>
            <th scope="col" className="py-2 pr-4 font-medium text-text-secondary">
              Value
            </th>
            <th scope="col" className="py-2 font-medium text-text-secondary">
              Usage
            </th>
          </tr>
        </thead>
        <tbody>
          {group.tokens.map((entry) => (
            <tr key={entry.token} className="border-b border-border-subtle last:border-b-0">
              <td className="py-2 pr-4">
                <span
                  aria-hidden="true"
                  className="block h-16 w-20 rounded-xs border border-border-default"
                  style={{ backgroundColor: `var(${entry.token})` }}
                />
              </td>
              <td className="py-2 pr-4 align-middle">
                <code className="rounded-xs bg-surface-sunken px-1.5 py-0.5 font-mono text-xs text-text-primary">
                  {entry.token}
                </code>
                <span className="mt-1 block text-xs text-text-tertiary">{entry.label}</span>
              </td>
              <td className="py-2 pr-4 align-middle font-mono text-xs text-text-secondary">{entry.value}</td>
              <td className="py-2 align-middle text-xs text-text-secondary">{entry.note}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/** Compact swatch grid used for the palette overview. */
export function ColorSwatchGrid() {
  const primitives = colorTokenGroups.filter((group) => group.layer === "primitive");

  return (
    <div className="space-y-8">
      {primitives.map((group) => (
        <div key={group.id}>
          <h4 className="mb-2 text-sm font-medium text-text-primary">{group.title}</h4>
          <ul className="flex flex-wrap gap-2">
            {group.tokens.map((entry) => (
              <li key={entry.token}>
                <div
                  className="flex h-20 w-28 flex-col justify-end rounded-sm border border-border-default p-2"
                  style={{
                    backgroundColor: `var(${entry.token})`,
                    color: isDarkToken(entry.token) ? "var(--rbi-white)" : "var(--rbi-off-black)",
                  }}
                >
                  <span className="text-2xs font-medium">{entry.label}</span>
                  <span className="font-mono text-2xs opacity-80">{entry.value}</span>
                </div>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
}

export interface ScaleTableProps {
  entries: readonly ScaleEntry[];
  /** Column heading for the usage column. */
  usageLabel?: string;
  /** Optional renderer for a visual preview of each entry. */
  renderSample?: (entry: ScaleEntry) => React.ReactNode;
  sampleLabel?: string;
}

export function ScaleTable({ entries, usageLabel = "Usage", renderSample, sampleLabel = "Sample" }: ScaleTableProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[40rem] border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-border-default">
            <th scope="col" className="py-2 pr-4 font-medium text-text-secondary">
              Token
            </th>
            <th scope="col" className="py-2 pr-4 font-medium text-text-secondary">
              Value
            </th>
            {renderSample ? (
              <th scope="col" className="py-2 pr-4 font-medium text-text-secondary">
                {sampleLabel}
              </th>
            ) : null}
            <th scope="col" className="py-2 font-medium text-text-secondary">
              {usageLabel}
            </th>
          </tr>
        </thead>
        <tbody>
          {entries.map((entry) => (
            <tr key={entry.token} className="border-b border-border-subtle last:border-b-0">
              <td className="py-2.5 pr-4 align-middle">
                <code className="rounded-xs bg-surface-sunken px-1.5 py-0.5 font-mono text-xs text-text-primary">
                  {entry.token}
                </code>
              </td>
              <td className="py-2.5 pr-4 align-middle font-mono text-xs text-text-secondary">{entry.value}</td>
              {renderSample ? <td className="py-2.5 pr-4 align-middle">{renderSample(entry)}</td> : null}
              <td className="py-2.5 align-middle text-xs text-text-secondary">{entry.usage}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

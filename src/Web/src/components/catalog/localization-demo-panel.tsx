import { useTranslation } from "react-i18next";

import { Text } from "@/components/ui/typography";
import { useFormatters, useLocalization } from "@/localization";

/**
 * Live proof that wording and formatting are runtime concerns.
 *
 * Every row below is resolved from the active localization release for the
 * active locale: ICU plurals and selects, and Intl number, currency, date,
 * relative-time and list formatting. Nothing here is a hardcoded string.
 */

const REFERENCE_TIMESTAMP = "2026-08-01T09:30:00Z";
const SAMPLE_LIST = ["EUR", "RON", "CZK"];

export function LocalizationDemoPanel() {
  const { t } = useTranslation("ui-library");
  const { t: tCommon } = useTranslation("common");
  const { locale, releaseId } = useLocalization();
  const formatters = useFormatters();

  // Stable ids keep React keys unique even when a label has no translation yet;
  // rows without wording are dropped instead of rendering an empty term.
  const rows: readonly { id: string; label: string; value: string }[] = [
    { id: "locale", label: t("demo.localeLabel"), value: locale },
    { id: "release", label: t("demo.releaseLabel"), value: releaseId ?? "—" },
    { id: "plural", label: t("demo.pluralLabel"), value: tCommon("localization.selectedItems", { count: 3 }) },
    { id: "select", label: t("demo.selectLabel"), value: tCommon("localization.accountStatus", { status: "review" }) },
    { id: "number", label: t("demo.numberLabel"), value: formatters.formatNumber(1234567.891) },
    { id: "currency", label: t("demo.currencyLabel"), value: formatters.formatCurrency(1250000, "EUR") },
    { id: "date", label: t("demo.dateLabel"), value: formatters.formatDateTime(REFERENCE_TIMESTAMP) },
    {
      id: "relative",
      label: t("demo.relativeLabel"),
      value: formatters.formatRelativeTime(REFERENCE_TIMESTAMP, new Date("2026-08-06T09:30:00Z")),
    },
    { id: "list", label: t("demo.listLabel"), value: formatters.formatList(SAMPLE_LIST) },
  ];

  return (
    <div className="glass rounded-sm border p-5">
      <p className="text-eyebrow text-text-tertiary">{t("demo.heading")}</p>
      <Text size="sm" tone="secondary" className="mt-2">
        {t("localization.formattingBody")}
      </Text>
      <dl className="mt-4 grid gap-x-6 gap-y-2 sm:grid-cols-2">
        {rows.filter((row) => row.label.length > 0).map((row) => (
          <div key={row.id} className="flex items-baseline justify-between gap-3 border-b border-border-subtle py-1.5">
            <dt className="text-sm text-text-tertiary">{row.label}</dt>
            <dd className="text-sm font-medium text-text-primary tabular-nums">{row.value}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}

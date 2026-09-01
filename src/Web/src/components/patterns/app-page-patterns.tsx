import { Download, Search, ShieldCheck } from "lucide-react";

import { useTranslation } from "react-i18next";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Heading, Text } from "@/components/ui/typography";
import {
  auditEntries,
  auditFilterKeys,
  profileFields,
  profileIdentity,
  profileToggles,
  releases,
  type AuditOutcome,
  type ReleaseKind,
} from "@/lib/patterns/app-pages";

/**
 * The three screens every application eventually needs.
 *
 * Presentation only: all record data comes from `src/lib/patterns/app-pages.ts`,
 * and every user-visible string resolves through the `patterns` localization
 * namespace, so a change to the example data or its copy never touches this file.
 */

const outcomeTones: Record<AuditOutcome, "success" | "warning" | "danger"> = {
  success: "success",
  warning: "warning",
  danger: "danger",
};

const releaseTones: Record<ReleaseKind, "brand" | "info" | "success"> = {
  added: "brand",
  changed: "info",
  fixed: "success",
};

/** Audit log: an immutable, filterable record of who did what. */
export function AuditLogPattern() {
  const { t } = useTranslation("patterns");

  return (
    <div className="w-full">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:max-w-xs">
          <Search
            aria-hidden="true"
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-text-tertiary"
          />
          <Input
            aria-label={t("auditPattern.searchAriaLabel")}
            placeholder={t("auditPattern.searchPlaceholder")}
            className="pl-9"
          />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {auditFilterKeys.map((filterKey, index) => (
            <Button key={filterKey} size="sm" variant={index === 0 ? "secondary" : "ghost"}>
              {t(`auditPattern.filters.${filterKey}` as never) as string}
            </Button>
          ))}
          <Button size="sm" variant="ghost">
            <Download aria-hidden="true" /> {t("auditPattern.export")}
          </Button>
        </div>
      </div>

      <div className="mt-4 overflow-x-auto rounded-sm border border-border-default">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("auditPattern.when")}</TableHead>
              <TableHead>{t("auditPattern.actor")}</TableHead>
              <TableHead>{t("auditPattern.action")}</TableHead>
              <TableHead>{t("auditPattern.target")}</TableHead>
              <TableHead>{t("auditPattern.outcome")}</TableHead>
              <TableHead>{t("auditPattern.sourceIp")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {auditEntries.map((entry) => (
              <TableRow key={`${entry.at}-${entry.actionKey}`}>
                <TableCell className="font-mono text-xs whitespace-nowrap text-text-secondary">{entry.at}</TableCell>
                <TableCell className="text-sm">{entry.actor}</TableCell>
                <TableCell className="text-sm font-medium text-text-primary">
                  {t(`auditPattern.entries.${entry.actionKey}` as never) as string}
                </TableCell>
                <TableCell className="text-sm text-text-secondary">
                  {t(`auditPattern.entries.${entry.targetKey}` as never) as string}
                </TableCell>
                <TableCell>
                  <Badge tone={outcomeTones[entry.outcome]} withDot>
                    {t(`auditPattern.outcomes.${entry.outcome}` as never) as string}
                  </Badge>
                </TableCell>
                <TableCell className="font-mono text-xs text-text-tertiary">{entry.ip}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <Text size="sm" tone="tertiary" className="mt-3">
        {t("auditPattern.appendOnlyNote")}
      </Text>
    </div>
  );
}

/** My profile: identity, read-only facts, and the switches a user owns. */
export function MyProfilePattern() {
  const { t } = useTranslation("patterns");

  return (
    <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,20rem)_minmax(0,1fr)]">
      <div className="glass rounded-sm border p-6">
        <div className="flex items-center gap-4">
          <Avatar className="size-14">
            <AvatarFallback className="text-base font-bold">{profileIdentity.initials}</AvatarFallback>
          </Avatar>
          <div className="min-w-0">
            <p className="truncate text-base font-bold text-text-primary">{profileIdentity.name}</p>
            <Text size="sm" tone="secondary">
              {t("profilePattern.identity.role")}
            </Text>
          </div>
        </div>
        <Text size="sm" tone="tertiary" className="mt-4">
          {t("profilePattern.identity.unit")}
        </Text>
        <div className="mt-4 flex items-center gap-2 rounded-sm border border-border-subtle p-3">
          <ShieldCheck aria-hidden="true" className="size-4 shrink-0 text-feedback-success" />
          <Text size="sm" tone="secondary">
            {t("profilePattern.verifiedAccount")}
          </Text>
        </div>
        <Button variant="secondary" size="sm" className="mt-4 w-full">
          {t("profilePattern.changePhoto")}
        </Button>
      </div>

      <div className="space-y-6">
        <div className="rounded-sm border border-border-default">
          <div className="flex items-center justify-between gap-3 border-b border-border-subtle px-4 py-3">
            <Heading level={3} size={6}>
              {t("profilePattern.details")}
            </Heading>
            <Button size="sm">{t("profilePattern.saveChanges")}</Button>
          </div>
          <dl className="divide-y divide-border-subtle">
            {profileFields.map((field) => (
              <div key={field.fieldKey} className="flex flex-col gap-1 px-4 py-3 sm:flex-row sm:items-center sm:gap-6">
                <dt className="text-sm text-text-secondary sm:w-48 sm:shrink-0">
                  {t(`profilePattern.fields.${field.fieldKey}` as never) as string}
                </dt>
                <dd className="flex min-w-0 flex-1 items-center gap-2 text-sm text-text-primary">
                  <span className="truncate">
                    {field.fieldKey === "interfaceLanguage"
                      ? t("profilePattern.fields.interfaceLanguageValue")
                      : field.value}
                  </span>
                  {field.editable ? null : <Badge tone="neutral">{t("profilePattern.readOnly")}</Badge>}
                </dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="rounded-sm border border-border-default">
          <div className="border-b border-border-subtle px-4 py-3">
            <Heading level={3} size={6}>
              {t("profilePattern.securityAndNotifications")}
            </Heading>
          </div>
          <ul className="divide-y divide-border-subtle">
            {profileToggles.map((toggle) => {
              const label = t(`profilePattern.toggles.${toggle.toggleKey}.label` as never) as string;
              return (
                <li key={toggle.toggleKey} className="flex items-start justify-between gap-4 px-4 py-3">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-text-primary">{label}</p>
                    <Text size="sm" tone="secondary" className="mt-0.5">
                      {t(`profilePattern.toggles.${toggle.toggleKey}.description` as never) as string}
                    </Text>
                  </div>
                  <Switch defaultChecked={toggle.enabled} aria-label={label} />
                </li>
              );
            })}
          </ul>
        </div>
      </div>
    </div>
  );
}

/** Release notes: a dated changelog with one badge per kind of change. */
export function ReleaseNotesPattern() {
  const { t } = useTranslation("patterns");

  return (
    <ol className="w-full space-y-4">
      {releases.map((release) => (
        <li
          key={release.version}
          className="rounded-sm border border-border-subtle p-5 sm:p-6"
          data-surface={release.current ? "subtle" : undefined}
        >
          <div className="flex flex-wrap items-baseline gap-3">
            <p className="font-brand text-xl font-bold tabular-nums text-text-primary">v{release.version}</p>
            {release.current ? <Badge tone="brand">{t("releaseNotesPattern.current")}</Badge> : null}
            <span className="font-mono text-xs text-text-tertiary">{release.date}</span>
          </div>
          <Heading level={3} size={6} className="mt-2">
            {t(`releaseNotesPattern.releases.${release.headlineKey}` as never) as string}
          </Heading>
          <ul className="mt-4 space-y-2">
            {release.changes.map((change) => (
              <li key={change.textKey} className="flex flex-wrap items-baseline gap-2">
                <Badge tone={releaseTones[change.kind]}>
                  {t(`releaseNotesPattern.kinds.${change.kind}` as never) as string}
                </Badge>
                <Text size="sm" tone="secondary" className="min-w-0">
                  {t(`releaseNotesPattern.releases.${change.textKey}` as never) as string}
                </Text>
              </li>
            ))}
          </ul>
        </li>
      ))}
    </ol>
  );
}

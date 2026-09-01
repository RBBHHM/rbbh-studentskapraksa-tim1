/**
 * The pages nearly every application ends up needing, described as data.
 *
 * Audit log, profile and release notes are not brand decisions — they are the
 * same three screens in every product, so their shape is declared once here and
 * the pattern components in `src/components/patterns/` only render it.
 *
 * All copy resolves through the `patterns` localization namespace at render time;
 * this module only carries the demo record data and the keys that resolve it.
 */

export type AuditOutcome = "success" | "warning" | "danger";

export interface AuditEntry {
  /** Fixed UTC timestamp so the server and browser render the same characters. */
  readonly at: string;
  readonly actor: string;
  readonly actionKey: string;
  readonly targetKey: string;
  readonly outcome: AuditOutcome;
  readonly ip: string;
}

export const auditEntries: readonly AuditEntry[] = [
  {
    at: "2026-08-06 14:12 UTC",
    actor: "h.mahmutovic@rbinternational.com",
    actionKey: "e1.action",
    targetKey: "e1.target",
    outcome: "success",
    ip: "10.24.8.19",
  },
  {
    at: "2026-08-06 13:58 UTC",
    actor: "service:payments-api",
    actionKey: "e2.action",
    targetKey: "e2.target",
    outcome: "warning",
    ip: "10.24.9.4",
  },
  {
    at: "2026-08-06 13:31 UTC",
    actor: "m.novak@rbinternational.com",
    actionKey: "e3.action",
    targetKey: "e3.target",
    outcome: "danger",
    ip: "88.200.14.77",
  },
  {
    at: "2026-08-06 12:47 UTC",
    actor: "m.novak@rbinternational.com",
    actionKey: "e4.action",
    targetKey: "e4.target",
    outcome: "success",
    ip: "10.24.8.51",
  },
  {
    at: "2026-08-06 11:05 UTC",
    actor: "h.mahmutovic@rbinternational.com",
    actionKey: "e5.action",
    targetKey: "e5.target",
    outcome: "success",
    ip: "10.24.8.19",
  },
];

export const auditFilterKeys = ["all", "signIns", "permissionChanges", "dataExports"] as const;

/* ------------------------------------------------------------------ profile */

export interface ProfileField {
  readonly fieldKey: string;
  readonly value: string;
  /** Fields the user may change themselves. */
  readonly editable: boolean;
}

export const profileIdentity = {
  name: "Hana Mahmutović",
  initials: "HK",
} as const;

export const profileFields: readonly ProfileField[] = [
  { fieldKey: "displayName", value: "Hana Mahmutović", editable: true },
  { fieldKey: "workEmail", value: "h.mahmutovic@rbinternational.com", editable: false },
  { fieldKey: "employeeId", value: "RBI-88214", editable: false },
  { fieldKey: "interfaceLanguage", value: "", editable: true },
  { fieldKey: "timeZone", value: "Europe/Vienna (UTC+2)", editable: true },
  { fieldKey: "lastSignIn", value: "2026-08-06 14:02 UTC", editable: false },
];

export interface ProfileToggle {
  readonly toggleKey: string;
  readonly enabled: boolean;
}

export const profileToggles: readonly ProfileToggle[] = [
  { toggleKey: "twoFactor", enabled: true },
  { toggleKey: "approvalEmails", enabled: true },
  { toggleKey: "weeklyDigest", enabled: false },
];

/* ------------------------------------------------------- release notes */

export type ReleaseKind = "added" | "changed" | "fixed";

export interface ReleaseEntry {
  readonly version: string;
  readonly date: string;
  readonly headlineKey: string;
  readonly current: boolean;
  readonly changes: readonly { readonly kind: ReleaseKind; readonly textKey: string }[];
}

export const releases: readonly ReleaseEntry[] = [
  {
    version: "2.4.0",
    date: "6 August 2026",
    headlineKey: "v240.headline",
    current: true,
    changes: [
      { kind: "added", textKey: "v240.changes.c1" },
      { kind: "added", textKey: "v240.changes.c2" },
      { kind: "changed", textKey: "v240.changes.c3" },
    ],
  },
  {
    version: "2.3.0",
    date: "29 July 2026",
    headlineKey: "v230.headline",
    current: false,
    changes: [
      { kind: "added", textKey: "v230.changes.c1" },
      { kind: "changed", textKey: "v230.changes.c2" },
      { kind: "fixed", textKey: "v230.changes.c3" },
    ],
  },
  {
    version: "2.2.0",
    date: "17 July 2026",
    headlineKey: "v220.headline",
    current: false,
    changes: [
      { kind: "added", textKey: "v220.changes.c1" },
      { kind: "added", textKey: "v220.changes.c2" },
      { kind: "fixed", textKey: "v220.changes.c3" },
    ],
  },
];

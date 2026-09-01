/**
 * Notification pattern data: the in-app notification panel's fixed record set.
 * Toasts triggered by the toast pattern build their copy from the same
 * severity vocabulary so both examples read as one system.
 */

export type NotificationSeverity = "info" | "success" | "warning" | "danger";

export interface NotificationEntry {
  readonly id: string;
  readonly severity: NotificationSeverity;
  readonly at: string;
  readonly read: boolean;
  readonly titleKey: string;
  readonly bodyKey: string;
}

export const notificationEntries: readonly NotificationEntry[] = [
  { id: "n1", severity: "warning", at: "2026-08-06 14:20 UTC", read: false, titleKey: "n1.title", bodyKey: "n1.body" },
  { id: "n2", severity: "success", at: "2026-08-06 11:04 UTC", read: false, titleKey: "n2.title", bodyKey: "n2.body" },
  { id: "n3", severity: "info", at: "2026-08-05 18:47 UTC", read: true, titleKey: "n3.title", bodyKey: "n3.body" },
  { id: "n4", severity: "danger", at: "2026-08-05 09:15 UTC", read: true, titleKey: "n4.title", bodyKey: "n4.body" },
];

export const toastDemoKeys = ["info", "success", "warning", "danger"] as const;

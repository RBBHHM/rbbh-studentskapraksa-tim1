import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { AlertTriangle, Bell, CheckCircle2, Info, OctagonAlert } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Text } from "@/components/ui/typography";
import { notificationEntries, toastDemoKeys, type NotificationSeverity } from "@/lib/patterns/notifications";

/** Toasts and the in-app notification panel — both driven by one severity vocabulary. */

const severityIcon: Record<NotificationSeverity, typeof Info> = {
  info: Info,
  success: CheckCircle2,
  warning: AlertTriangle,
  danger: OctagonAlert,
};

const severityTone: Record<NotificationSeverity, "info" | "success" | "warning" | "danger"> = {
  info: "info",
  success: "success",
  warning: "warning",
  danger: "danger",
};

export function ToastPattern() {
  const { t } = useTranslation("patterns");

  function fire(severity: NotificationSeverity) {
    const title = t(`notificationsPattern.toasts.${severity}.title` as never) as string;
    const description = t(`notificationsPattern.toasts.${severity}.body` as never) as string;
    if (severity === "success") toast.success(title, { description });
    else if (severity === "warning") toast.warning(title, { description });
    else if (severity === "danger") toast.error(title, { description });
    else toast(title, { description });
  }

  return (
    <div className="flex w-full flex-wrap gap-3">
      {toastDemoKeys.map((key) => (
        <Button key={key} variant="secondary" onClick={() => fire(key)}>
          {t(`notificationsPattern.toasts.${key}.trigger` as never) as string}
        </Button>
      ))}
    </div>
  );
}

export function NotificationPanelPattern() {
  const { t } = useTranslation("patterns");
  const [readIds, setReadIds] = useState<readonly string[]>(
    notificationEntries.filter((n) => n.read).map((n) => n.id),
  );
  const unreadCount = notificationEntries.length - readIds.length;

  return (
    <div className="mx-auto w-full max-w-md rounded-sm border border-border-default">
      <div className="flex items-center justify-between gap-3 border-b border-border-subtle px-4 py-3">
        <div className="flex items-center gap-2">
          <Bell aria-hidden="true" className="size-4 text-text-tertiary" />
          <p className="text-sm font-bold text-text-primary">{t("notificationsPattern.panel.title")}</p>
          {unreadCount > 0 ? <Badge tone="brand">{unreadCount}</Badge> : null}
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setReadIds(notificationEntries.map((n) => n.id))}
          disabled={unreadCount === 0}
        >
          {t("notificationsPattern.panel.markAllRead")}
        </Button>
      </div>
      <ul className="divide-y divide-border-subtle">
        {notificationEntries.map((entry) => {
          const isRead = readIds.includes(entry.id);
          const Icon = severityIcon[entry.severity];
          return (
            <li key={entry.id} className="flex items-start gap-3 px-4 py-3" data-surface={isRead ? undefined : "subtle"}>
              <Icon aria-hidden="true" className={`mt-0.5 size-4 shrink-0 text-feedback-${entry.severity}`} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate text-sm font-medium text-text-primary">
                    {t(`notificationsPattern.entries.${entry.titleKey}` as never) as string}
                  </p>
                  <Badge tone={severityTone[entry.severity]} withDot>
                    {t(`notificationsPattern.severities.${entry.severity}` as never) as string}
                  </Badge>
                </div>
                <Text size="sm" tone="secondary" className="mt-0.5">
                  {t(`notificationsPattern.entries.${entry.bodyKey}` as never) as string}
                </Text>
                <div className="mt-1 flex items-center gap-3">
                  <span className="font-mono text-2xs text-text-tertiary">{entry.at}</span>
                  {!isRead ? (
                    <Button
                      variant="link"
                      size="sm"
                      className="h-auto px-0 text-2xs"
                      onClick={() => setReadIds((current) => [...current, entry.id])}
                    >
                      {t("notificationsPattern.panel.markRead")}
                    </Button>
                  ) : null}
                </div>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

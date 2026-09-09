import { BadgeCheck, KeyRound, Mail, ShieldCheck, UserRound } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Heading, Text } from "@/components/ui/typography";
import { getCurrentUser } from "@/lib/auth/current-user";
import { applicationAccessRoles } from "@/lib/auth/application-access";

export function ProfilePage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const user = getCurrentUser();
  const effectiveRoles = (user?.roles ?? []).filter((role) => role.toLowerCase() !== "gost" && applicationAccessRoles.includes(role.toLowerCase() as (typeof applicationAccessRoles)[number]));
  const displayName = user?.fullName ?? user?.username ?? "—";
  const username = user?.username ?? "—";
  const email = user?.email ?? "—";
  const permissions = user?.permissions ?? [];

  return (
    <section className="mx-auto max-w-5xl">
      <div className="rounded-sm border border-border-subtle bg-surface-default p-6 shadow-sm sm:p-8">
        <div className="flex flex-col gap-5 sm:flex-row sm:items-center">
          <div className="grid size-20 shrink-0 place-items-center rounded-full bg-surface-brand text-text-on-brand"><UserRound className="size-9" /></div>
          <div className="min-w-0">
            <Text tone="secondary">{bs ? "Trenutno prijavljeni korisnik" : "Currently signed-in user"}</Text>
            <Heading level={1} size={4} className="mt-1 break-words">{displayName}</Heading>
            <p className="mt-2 flex items-center gap-2 text-sm text-text-secondary"><KeyRound className="size-4" />{username}</p>
            <p className="mt-1 flex items-center gap-2 break-all text-sm text-text-secondary"><Mail className="size-4" />{email}</p>
          </div>
        </div>
      </div>
      <div className="mt-6 grid gap-5 lg:grid-cols-2">
        <div className="rounded-sm border border-border-subtle bg-surface-default p-6 shadow-sm">
          <Heading level={2} size={3}>{bs ? "Dodijeljene uloge" : "Assigned roles"}</Heading>
          <div className="mt-4 flex flex-wrap gap-2">{effectiveRoles.length ? effectiveRoles.map((role) => <span key={role} className="rounded-full bg-surface-muted px-3 py-1.5 text-sm font-semibold">{accessLabel(role, bs)}</span>) : <Text tone="secondary">{bs ? "Nema dodijeljenih funkcionalnih pristupa." : "No functional access assigned."}</Text>}</div>
        </div>
        <div className="rounded-sm border border-border-subtle bg-surface-default p-6 shadow-sm">
          <Heading level={2} size={3}>{bs ? "Efektivna ovlaštenja" : "Effective permissions"}</Heading>
          <ul className="mt-4 space-y-3">{permissions.map((permission) => <li key={permission} className="flex gap-3 text-sm"><BadgeCheck className="mt-0.5 size-4 shrink-0 text-feedback-success" />{permission}</li>)}</ul>
        </div>
      </div>
      <div className="mt-5 flex items-start gap-3 rounded-sm border border-border-subtle bg-surface-subtle p-4 text-sm text-text-secondary"><ShieldCheck className="mt-0.5 size-4 shrink-0" />{bs ? "Ovlaštenja se izračunavaju iz uloga aktivne sesije. Backend pri svakoj zaštićenoj operaciji ponovo provjerava pristup." : "Permissions are derived from active-session roles. The backend checks access again for every protected operation."}</div>
    </section>
  );
}

function accessLabel(role: string, bs: boolean) {
  const labels: Record<string, [string, string]> = {
    fl: ["Fizička lica", "Individuals"],
    pl: ["Pravna lica", "Legal entities"],
    limiti: ["Limiti", "Limits"],
    kapital: ["Kapital", "Capital"],
    admin: ["Administrator", "Administrator"],
    gost: ["Gost", "Guest"],
  };
  return labels[role.toLowerCase()]?.[bs ? 0 : 1] ?? role;
}

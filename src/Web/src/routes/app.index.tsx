import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowRight, CheckCircle2, ShieldCheck } from "lucide-react";
import { useTranslation } from "react-i18next";

import { Heading, Text } from "@/components/ui/typography";
import { BentoCard, BentoGrid } from "@/components/ui/bento-grid";
import { HeroSection } from "@/components/ui/hero-section";
import { registryResources } from "@/lib/registry/resources";
import { hasAllApplicationAccesses, hasApplicationAccess } from "@/lib/auth/application-access";

export const Route = createFileRoute("/app/")({ component: Dashboard });

function Dashboard() {
  const { t, i18n } = useTranslation("registry");
  const bs = i18n.language.startsWith("bs");
  const resources = registryResources.filter((item) =>
    item.key !== "dashboard" &&
    hasApplicationAccess(item.accessRole) &&
    (!item.requiresAllAccesses || hasAllApplicationAccesses()));
  const work = resources.filter((item) => item.area === "work" && item.accessRole);
  const supporting = resources.filter((item) => !item.accessRole);
  return (
    <section>
      <HeroSection
        eyebrow={t("dashboard.eyebrow")}
        title={t("dashboard.title")}
        lead={t("dashboard.description")}
        footer={<><span className="inline-flex items-center gap-2"><CheckCircle2 className="size-4" />{bs ? "4 nezavisna funkcionalna pristupa" : "4 independent functional accesses"}</span><span>{bs ? "Praćenje statusa i promjena" : "Status and change tracking"}</span><span>{bs ? "Kontrolisan mjesečni period" : "Controlled monthly period"}</span></>}
      />
      <div className="mt-10 flex items-end justify-between gap-4">
        <div><p className="text-eyebrow text-text-tertiary">{bs ? "Poslovna područja" : "Business areas"}</p><Heading level={2} size={3} className="mt-2">{bs ? "Nastavite tamo gdje radite" : "Continue where you work"}</Heading></div>
      </div>
      <BentoGrid className="mt-5" density="tall" gap="loose">
        {work.map((resource) => (
          <BentoCard key={resource.key} span={6} rowSpan={1} tone="subtle" accent className="group min-h-64 hover:border-border-brand hover:shadow-md">
            <Link to={resource.path} className="absolute inset-0 z-10" aria-label={`${t("actions.open")} ${t(`resources.${resource.key}.title` as never)}`} />
            <div className="flex items-start justify-between gap-4"><span className="grid size-12 place-items-center rounded-sm border border-border-subtle bg-surface-default text-text-primary"><resource.icon className="size-6" /></span><ArrowRight className="size-5 text-text-primary transition-transform group-hover:translate-x-1" /></div>
            <h2 className="mt-auto pt-10 text-xl font-bold">{t(`resources.${resource.key}.title` as never)}</h2>
            <p className="mt-2 max-w-prose text-sm text-text-secondary">{t(`resources.${resource.key}.description` as never)}</p>
          </BentoCard>
        ))}
      </BentoGrid>
      {supporting.length > 0 && <div className="mt-10 rounded-sm border border-border-subtle bg-surface-subtle p-5"><div className="flex items-center gap-2"><ShieldCheck className="size-5" /><h2 className="font-bold">{bs ? "Podrška i administracija" : "Support and administration"}</h2></div><div className="mt-4 flex flex-wrap gap-2">{supporting.map((resource) => <Link key={resource.key} to={resource.path} className="rounded-sm border border-border-subtle bg-surface-default px-3 py-2 text-sm font-semibold hover:border-border-brand">{t(`resources.${resource.key}.title` as never)}</Link>)}</div></div>}
    </section>
  );
}

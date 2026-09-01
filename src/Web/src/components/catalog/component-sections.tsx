import * as React from "react";
import { useTranslation } from "react-i18next";
import {
  Building2,
  CreditCard,
  FileText,
  Gauge,
  Layers,
  Settings,
  ShieldCheck,
  TrendingUp,
  Users,
  Wallet,
} from "lucide-react";

import { CatalogSection, CatalogSubsection } from "@/components/catalog/catalog-page";
import { A11yNotes, Example } from "@/components/catalog/example";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { Dock, dockItemClasses } from "@/components/layout/dock";
import { SideNav, type SideNavSection } from "@/components/ui/side-nav";
import { Skeleton } from "@/components/ui/skeleton";
import { Stat, StatGroup } from "@/components/ui/stat";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Text } from "@/components/ui/typography";

/**
 * Catalog sections for navigation, statistics and status components.
 *
 * Kept out of the route file so the route stays a table of contents rather than
 * a thousand-line page, and so each showcase owns its own interactive state.
 */

function getSideNavSections(t: (key: any) => any): readonly SideNavSection[] {
  return [
    {
      id: "operate",
      label: t("navigationSection.sideNav.sections.operate"),
      items: [
        { id: "dashboard", label: t("navigationSection.sideNav.sections.dashboard"), icon: Gauge },
        { id: "accounts", label: t("navigationSection.sideNav.sections.accounts"), icon: Building2, badge: "24" },
        { id: "payments", label: t("navigationSection.sideNav.sections.payments"), icon: CreditCard },
        {
          id: "lending",
          label: t("navigationSection.sideNav.sections.lending"),
          icon: Wallet,
          items: [
            { id: "lending-pipeline", label: t("navigationSection.sideNav.sections.lendingPipeline") },
            { id: "lending-limits", label: t("navigationSection.sideNav.sections.lendingLimits") },
            { id: "lending-collateral", label: t("navigationSection.sideNav.sections.lendingCollateral") },
          ],
        },
      ],
    },
    {
      id: "govern",
      label: t("navigationSection.sideNav.sections.govern"),
      items: [
        { id: "users", label: t("navigationSection.sideNav.sections.users"), icon: Users },
        { id: "compliance", label: t("navigationSection.sideNav.sections.compliance"), icon: ShieldCheck, badge: "3" },
        { id: "reports", label: t("navigationSection.sideNav.sections.reports"), icon: FileText },
        { id: "settings", label: t("navigationSection.sideNav.sections.settings"), icon: Settings },
      ],
    },
  ];
}

const sideNavCode = `const sections = [
  {
    id: "operate",
    label: "Operate",
    items: [
      { id: "dashboard", label: "Dashboard", icon: Gauge },
      { id: "accounts", label: "Accounts", icon: Building2, badge: "24" },
      { id: "lending", label: "Lending", icon: Wallet, items: [{ id: "lending-limits", label: "Limits" }] },
    ],
  },
];

<SideNav
  label="Back-office navigation"
  sections={sections}
  activeId={activeId}
  onSelect={setActiveId}
  collapsible
  header={<p className="text-sm font-bold">Back office</p>}
/>`;

function SideNavShowcase() {
  const { t } = useTranslation("components");
  const sideNavSections = React.useMemo(() => getSideNavSections(t), [t]);
  const [activeId, setActiveId] = React.useState("accounts");
  const [collapsed, setCollapsed] = React.useState(false);

  return (
    <Example
      title={t("navigationSection.sideNav.title")}
      description={t("navigationSection.sideNav.description")}
      surface="default"
      className="block"
      code={sideNavCode}
    >
      <div className="flex w-full flex-col gap-4 sm:flex-row">
        <SideNav
          label={t("navigationSection.sideNav.ariaLabel")}
          sections={sideNavSections}
          activeId={activeId}
          onSelect={setActiveId}
          collapsed={collapsed}
          onCollapsedChange={setCollapsed}
          header={
            <div className="min-w-0">
              <p className="truncate text-sm font-bold text-text-primary">{t("navigationSection.sideNav.headerTitle")}</p>
              <p className="truncate text-xs text-text-tertiary">{t("navigationSection.sideNav.headerSubtitle")}</p>
            </div>
          }
          footer={
            <div className="flex min-w-0 items-center gap-2">
              <Avatar className="size-8 shrink-0">
                <AvatarFallback>HK</AvatarFallback>
              </Avatar>
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-text-primary">{t("navigationSection.sideNav.footerName")}</p>
                <p className="truncate text-xs text-text-tertiary">{t("navigationSection.sideNav.footerRole")}</p>
              </div>
            </div>
          }
        />

        <div className="min-w-0 flex-1 rounded-sm border border-border-subtle bg-surface-subtle p-6">
          <Text size="sm" tone="secondary">
            {t("navigationSection.sideNav.selectedItem")}
          </Text>
          <p className="mt-1 font-brand text-lg font-bold text-text-primary">{activeId}</p>
          <Button variant="secondary" className="mt-4" onClick={() => setCollapsed((value) => !value)}>
            {collapsed ? t("navigationSection.sideNav.expandRail") : t("navigationSection.sideNav.collapseToIcons")}
          </Button>
        </div>
      </div>
    </Example>
  );
}

function DrawerNavShowcase() {
  const { t } = useTranslation("components");
  const sideNavSections = React.useMemo(() => getSideNavSections(t), [t]);
  return (
    <Example
      title={t("navigationSection.drawer.title")}
      description={t("navigationSection.drawer.description")}
      code={`<Sheet>
  <SheetTrigger asChild><Button variant="secondary">Open navigation</Button></SheetTrigger>
  <SheetContent side="left"><SideNav … collapsible={false} /></SheetContent>
</Sheet>`}
    >
      <Sheet>
        <SheetTrigger asChild>
          <Button variant="secondary">
            <Layers aria-hidden="true" /> {t("navigationSection.drawer.open")}
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="w-[19rem] p-0">
          <SheetHeader className="border-b border-border-subtle p-4">
            <SheetTitle>{t("navigationSection.drawer.sheetTitle")}</SheetTitle>
            <SheetDescription>{t("navigationSection.drawer.sheetDescription")}</SheetDescription>
          </SheetHeader>
          <div className="p-3">
            <SideNav
              label={t("navigationSection.sideNav.ariaLabel")}
              sections={sideNavSections}
              activeId="accounts"
              onSelect={() => undefined}
              collapsible={false}
              className="border-0 bg-transparent"
            />
          </div>
        </SheetContent>
      </Sheet>
    </Example>
  );
}

function DockShowcase() {
  const { t } = useTranslation("components");
  const items = [
    { id: "admin", label: t("navigationSection.dock.items.admin"), icon: Gauge },
    { id: "api", label: t("navigationSection.dock.items.api"), icon: Layers },
    { id: "audit", label: t("navigationSection.dock.items.audit"), icon: ShieldCheck },
  ] as const;
  const [activeId, setActiveId] = React.useState<string>("admin");

  return (
    <Example
      title={t("navigationSection.dock.title")}
      description={t("navigationSection.dock.description")}
      surface="subtle"
      className="block"
      code={`<Dock label="Application examples">
  {items.map((item) => (
    <li key={item.id}>
      <button
        type="button"
        aria-current={item.id === activeId ? "page" : undefined}
        className={dockItemClasses(item.id === activeId)}
        onClick={() => setActiveId(item.id)}
      >
        <item.icon aria-hidden="true" className="size-4" />
        {item.label}
      </button>
    </li>
  ))}
</Dock>`}
    >
      <Dock label={t("navigationSection.dock.ariaLabel")}>
        {items.map((item) => {
          const Icon = item.icon;
          const active = item.id === activeId;
          return (
            <li key={item.id}>
              <button
                type="button"
                aria-current={active ? "page" : undefined}
                className={dockItemClasses(active)}
                onClick={() => setActiveId(item.id)}
              >
                <Icon aria-hidden="true" className="size-4" />
                {item.label}
              </button>
            </li>
          );
        })}
      </Dock>
    </Example>
  );
}

export function NavigationSection() {
  const { t } = useTranslation("components");
  return (
    <CatalogSection
      id="navigation"
      title={t("navigationSection.title")}
      description={t("navigationSection.description")}
    >
      <div className="space-y-4">
        <SideNavShowcase />
        <DrawerNavShowcase />

        <Example
          title={t("navigationSection.breadcrumb.title")}
          surface="default"
          className="block"
          code={`<Breadcrumb>
  <BreadcrumbList>
    <BreadcrumbItem><BreadcrumbLink href="/">Overview</BreadcrumbLink></BreadcrumbItem>
    <BreadcrumbSeparator />
    <BreadcrumbItem><BreadcrumbPage>Accounts</BreadcrumbPage></BreadcrumbItem>
  </BreadcrumbList>
</Breadcrumb>`}
        >
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink href="#navigation">{t("navigationSection.breadcrumb.overview")}</BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                <BreadcrumbLink href="#navigation">{t("navigationSection.breadcrumb.corporateBanking")}</BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                <BreadcrumbPage>{t("navigationSection.breadcrumb.account")}</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </Example>

        <Example
          title={t("navigationSection.pagination.title")}
          surface="default"
          className="block"
          code={`<Pagination>
  <PaginationContent>
    <PaginationItem><PaginationPrevious href="#" /></PaginationItem>
    <PaginationItem><PaginationLink href="#" isActive>2</PaginationLink></PaginationItem>
    <PaginationItem><PaginationNext href="#" /></PaginationItem>
  </PaginationContent>
</Pagination>`}
        >
          <Pagination>
            <PaginationContent>
              <PaginationItem>
                <PaginationPrevious href="#navigation" />
              </PaginationItem>
              <PaginationItem>
                <PaginationLink href="#navigation">1</PaginationLink>
              </PaginationItem>
              <PaginationItem>
                <PaginationLink href="#navigation" isActive>
                  2
                </PaginationLink>
              </PaginationItem>
              <PaginationItem>
                <PaginationLink href="#navigation">3</PaginationLink>
              </PaginationItem>
              <PaginationItem>
                <PaginationEllipsis />
              </PaginationItem>
              <PaginationItem>
                <PaginationNext href="#navigation" />
              </PaginationItem>
            </PaginationContent>
          </Pagination>
        </Example>
      </div>

      <A11yNotes
        items={[
          {
            key: "aria-current",
            behaviour: t("navigationSection.a11y.ariaCurrent"),
          },
          {
            key: "Enter / Space",
            behaviour: t("navigationSection.a11y.enterSpace"),
          },
          {
            key: "Escape",
            behaviour: t("navigationSection.a11y.escape"),
          },
        ]}
      />
    </CatalogSection>
  );
}

export function StatisticsSection() {
  const { t } = useTranslation("components");
  return (
    <CatalogSection
      id="statistics"
      title={t("statisticsSection.title")}
      description={t("statisticsSection.description")}
    >
      <div className="space-y-4">
        <Example
          title={t("statisticsSection.variantsTitle")}
          surface="default"
          className="block"
          code={`<StatGroup columns={4} label="Portfolio">
  <Stat variant="rule" label="Markets" value="17" hint="Central and Eastern Europe" />
  <Stat variant="card" label="Exposure" value="EUR 4.2bn" trend={{ direction: "up", value: "+4.2%", caption: "vs. Q3" }} />
  <Stat variant="panel" label="Customers" value="17.8m" />
  <Stat variant="plain" label="Since" value="1927" />
</StatGroup>`}
        >
          <StatGroup columns={4} gap="default" label={t("statisticsSection.portfolioLabel")} className="w-full">
            <Stat variant="rule" size="sm" label={t("statisticsSection.markets")} value="17" hint={t("statisticsSection.marketsHint")} />
            <Stat
              variant="card"
              size="sm"
              label={t("statisticsSection.exposure")}
              value="EUR 4.2bn"
              icon={TrendingUp}
              trend={{ direction: "up", value: "+4.2%", caption: "vs. Q3" }}
            />
            <Stat
              variant="panel"
              size="sm"
              label={t("statisticsSection.customers")}
              value="17.8m"
              trend={{ direction: "down", value: "-0.6%", caption: "vs. Q3" }}
            />
            <Stat variant="plain" size="sm" label={t("statisticsSection.since")} value="1927" hint={t("statisticsSection.sinceHint")} />
          </StatGroup>
        </Example>

        <CatalogSubsection
          title={t("statisticsSection.subsectionTitle")}
          description={t("statisticsSection.subsectionDescription")}
        >
          <div className="space-y-4">
            {(["subtle", "brand", "corporate", "inverse"] as const).map((surface) => (
              <Example key={surface} title={t("sections.surfaces.exampleTitle", { surface }) as string} surface={surface} className="block">
                <StatGroup columns={3} label={t("statisticsSection.surfaceLabel", { surface }) as string} className="w-full">
                  <Stat variant="rule" label={t("statisticsSection.markets")} value="17" hint={t("statisticsSection.marketsHintShort")} />
                  <Stat variant="rule" label={t("statisticsSection.employees")} value="44,000" hint={t("statisticsSection.employeesHint")} />
                  <Stat
                    variant="rule"
                    label={t("statisticsSection.customers")}
                    value="17.8m"
                    trend={{ direction: "up", value: "+1.1%" }}
                  />
                </StatGroup>
              </Example>
            ))}
          </div>
        </CatalogSubsection>
      </div>

      <A11yNotes
        items={[
          {
            key: "Grouping",
            behaviour: t("statisticsSection.a11y.grouping"),
          },
          {
            key: "Trend",
            behaviour: t("statisticsSection.a11y.trend"),
          },
          {
            key: "Numerals",
            behaviour: t("statisticsSection.a11y.numerals"),
          },
        ]}
      />
    </CatalogSection>
  );
}

export function StatusSection() {
  return (
    <CatalogSection
      id="status"
      title="Status and progress"
      description="The small parts every screen ends up needing: determinate progress, loading placeholders, identity and a hint on hover or focus."
    >
      <div className="space-y-4">
        <Example
          title="Progress"
          surface="default"
          className="block"
          code={`<Progress value={68} aria-label="Onboarding completion" />`}
        >
          <div className="w-full max-w-md space-y-2">
            <div className="flex items-baseline justify-between gap-3">
              <span className="text-sm font-medium text-text-primary">Onboarding completion</span>
              <span className="font-mono text-sm tabular-nums text-text-secondary">68%</span>
            </div>
            <Progress value={68} aria-label="Onboarding completion: 68 percent" />
          </div>
        </Example>

        <Example
          title="Skeleton"
          surface="default"
          className="block"
          code={`<Skeleton className="h-4 w-40" />`}
        >
          <div className="w-full max-w-md space-y-3" aria-hidden="true">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
          </div>
        </Example>

        <Example
          title="Avatar, separator and tooltip"
          surface="default"
          code={`<Avatar><AvatarFallback>HK</AvatarFallback></Avatar>
<Separator orientation="vertical" />
<Tooltip><TooltipTrigger asChild><Button variant="ghost">Details</Button></TooltipTrigger>
  <TooltipContent>Shown on hover and on keyboard focus.</TooltipContent></Tooltip>`}
        >
          <Avatar>
            <AvatarFallback>HK</AvatarFallback>
          </Avatar>
          <div>
            <p className="text-sm font-bold text-text-primary">Hana Mahmutović</p>
            <p className="text-xs text-text-tertiary">Administrator · Group operations</p>
          </div>
          <Separator orientation="vertical" className="h-10" />
          <Badge tone="success" withDot>
            Active
          </Badge>
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="ghost">Details</Button>
              </TooltipTrigger>
              <TooltipContent>Appears on hover and on keyboard focus, and stays while pointed at.</TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </Example>
      </div>

      <A11yNotes
        items={[
          {
            key: "role=progressbar",
            behaviour: "Progress exposes its value; the visible percentage is duplicated in the accessible name.",
          },
          {
            key: "Skeletons",
            behaviour:
              "Marked aria-hidden and paired with a polite live region elsewhere, so loading is announced once rather than as a wall of empty boxes.",
          },
          {
            key: "Focus",
            behaviour: "Tooltips open on keyboard focus, not only on hover, and never contain interactive content.",
          },
        ]}
      />
    </CatalogSection>
  );
}

import * as React from "react";
import { ChevronDown, PanelLeftClose, PanelLeftOpen, type LucideIcon } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/**
 * SideNav — the design system's in-flow sidebar navigation.
 *
 * Unlike an app-chrome sidebar it is laid out in normal document flow, so it
 * can be used as a page rail, inside a dashboard shell, or embedded in a
 * documentation example. It collapses to a 3.5rem icon rail rather than
 * disappearing, which keeps navigation reachable at every width.
 *
 * Accessibility:
 * - Rendered as a `nav` with an accessible name; the active item carries
 *   `aria-current="page"` in addition to the yellow indicator.
 * - Groups are real disclosure buttons with `aria-expanded`.
 * - Collapsed items keep their text as an accessible name and a `title`, so the
 *   icon rail is never unlabelled.
 */

export interface SideNavItem {
  readonly id: string;
  readonly label: string;
  readonly icon?: LucideIcon | undefined;
  /** Small trailing count or status, e.g. an unread badge. */
  readonly badge?: string | undefined;
  readonly items?: readonly SideNavItem[] | undefined;
}

export interface SideNavSection {
  readonly id: string;
  readonly label?: string | undefined;
  readonly items: readonly SideNavItem[];
}

export interface SideNavProps {
  readonly label: string;
  readonly sections: readonly SideNavSection[];
  readonly activeId: string;
  readonly onSelect: (id: string) => void;
  /** Shows the collapse control and the icon rail. */
  readonly collapsible?: boolean | undefined;
  readonly collapsed?: boolean | undefined;
  readonly onCollapsedChange?: ((collapsed: boolean) => void) | undefined;
  readonly header?: React.ReactNode;
  readonly footer?: React.ReactNode;
  readonly className?: string | undefined;
}

function flatten(items: readonly SideNavItem[]): readonly SideNavItem[] {
  return items.flatMap((item) => [item, ...flatten(item.items ?? [])]);
}

export function SideNav({
  label,
  sections,
  activeId,
  onSelect,
  collapsible = true,
  collapsed: controlledCollapsed,
  onCollapsedChange,
  header,
  footer,
  className,
}: SideNavProps) {
  const [uncontrolledCollapsed, setUncontrolledCollapsed] = React.useState(false);
  const collapsed = controlledCollapsed ?? uncontrolledCollapsed;

  function setCollapsed(next: boolean) {
    setUncontrolledCollapsed(next);
    onCollapsedChange?.(next);
  }

  /** A group stays open while it contains the active item. */
  const groupsWithActive = React.useMemo(() => {
    const open = new Set<string>();
    for (const section of sections) {
      for (const item of section.items) {
        if (item.items && flatten(item.items).some((child) => child.id === activeId)) open.add(item.id);
      }
    }
    return open;
  }, [sections, activeId]);

  const [openGroups, setOpenGroups] = React.useState<ReadonlySet<string>>(groupsWithActive);
  React.useEffect(() => {
    setOpenGroups((current) => new Set([...current, ...groupsWithActive]));
  }, [groupsWithActive]);

  function toggleGroup(id: string) {
    setOpenGroups((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  return (
    <nav
      aria-label={label}
      data-collapsed={collapsed || undefined}
      className={cn(
        "flex shrink-0 flex-col border border-border-subtle bg-surface-subtle",
        "rounded-sm transition-[width] duration-200 ease-standard",
        collapsed ? "w-14" : "w-full sm:w-64",
        className,
      )}
    >
      {header || collapsible ? (
        <div className="flex min-h-14 items-center justify-between gap-2 border-b border-border-subtle px-2">
          {collapsed ? null : <div className="min-w-0 flex-1 px-1">{header}</div>}
          {collapsible ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-expanded={!collapsed}
              aria-label={collapsed ? "Expand navigation" : "Collapse navigation"}
              onClick={() => setCollapsed(!collapsed)}
            >
              {collapsed ? <PanelLeftOpen aria-hidden="true" /> : <PanelLeftClose aria-hidden="true" />}
            </Button>
          ) : null}
        </div>
      ) : null}

      <div className="flex-1 overflow-y-auto p-2">
        {sections.map((section) => (
          <div key={section.id} className="mb-4 last:mb-0">
            {section.label && !collapsed ? (
              <p className="text-eyebrow px-2 py-1.5 text-text-tertiary">{section.label}</p>
            ) : null}
            <ul className="space-y-0.5">
              {section.items.map((item) => (
                <SideNavNode
                  key={item.id}
                  item={item}
                  activeId={activeId}
                  collapsed={collapsed}
                  depth={0}
                  open={openGroups.has(item.id)}
                  onToggle={toggleGroup}
                  onSelect={onSelect}
                />
              ))}
            </ul>
          </div>
        ))}
      </div>

      {footer && !collapsed ? (
        <div className="border-t border-border-subtle p-3">{footer}</div>
      ) : null}
    </nav>
  );
}

const itemClasses = [
  "group/item relative flex w-full min-h-10 items-center gap-2.5 rounded-xs px-2 text-sm",
  "text-text-secondary transition-colors duration-150 ease-standard",
  "hover:bg-surface hover:text-text-primary",
] as const;

function SideNavNode({
  item,
  activeId,
  collapsed,
  depth,
  open,
  onToggle,
  onSelect,
}: {
  item: SideNavItem;
  activeId: string;
  collapsed: boolean;
  depth: number;
  open: boolean;
  onToggle: (id: string) => void;
  onSelect: (id: string) => void;
}) {
  const Icon = item.icon;
  const hasChildren = Boolean(item.items?.length);
  const isActive = activeId === item.id;
  const containsActive = hasChildren && flatten(item.items ?? []).some((child) => child.id === activeId);

  if (hasChildren) {
    return (
      <li>
        <button
          type="button"
          aria-expanded={open}
          title={collapsed ? item.label : undefined}
          onClick={() => (collapsed ? onToggle(item.id) : onToggle(item.id))}
          className={cn(
            itemClasses,
            containsActive && "font-medium text-text-primary",
            collapsed && "justify-center px-0",
          )}
        >
          {Icon ? <Icon aria-hidden="true" className="size-4 shrink-0" /> : null}
          {collapsed ? (
            <span className="sr-only">{item.label}</span>
          ) : (
            <>
              <span className="min-w-0 flex-1 truncate text-left">{item.label}</span>
              <ChevronDown
                aria-hidden="true"
                className={cn(
                  "size-4 shrink-0 transition-transform duration-150 ease-standard",
                  open && "rotate-180",
                )}
              />
            </>
          )}
        </button>

        {open && !collapsed ? (
          <ul className="mt-0.5 ml-4 space-y-0.5 border-l border-border-subtle pl-2">
            {item.items?.map((child) => (
              <SideNavNode
                key={child.id}
                item={child}
                activeId={activeId}
                collapsed={collapsed}
                depth={depth + 1}
                open={false}
                onToggle={onToggle}
                onSelect={onSelect}
              />
            ))}
          </ul>
        ) : null}
      </li>
    );
  }

  return (
    <li>
      <button
        type="button"
        aria-current={isActive ? "page" : undefined}
        title={collapsed ? item.label : undefined}
        onClick={() => onSelect(item.id)}
        className={cn(
          itemClasses,
          isActive && "bg-surface font-bold text-text-primary",
          collapsed && "justify-center px-0",
        )}
      >
        {isActive ? (
          <span aria-hidden="true" className="absolute inset-y-1 left-0 w-[3px] rounded-full bg-surface-brand" />
        ) : null}
        {Icon ? <Icon aria-hidden="true" className="size-4 shrink-0" /> : null}
        {collapsed ? (
          <span className="sr-only">{item.label}</span>
        ) : (
          <>
            <span className="min-w-0 flex-1 truncate text-left">{item.label}</span>
            {item.badge ? (
              <Badge tone="neutral" className="shrink-0">
                {item.badge}
              </Badge>
            ) : null}
          </>
        )}
      </button>
    </li>
  );
}

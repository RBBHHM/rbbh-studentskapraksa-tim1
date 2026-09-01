import { useQuery } from "@tanstack/react-query";
import { Network, UserRound } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";

import { IconIndicator } from "@/components/registry/icon-indicator";
import { Heading, Text } from "@/components/ui/typography";
import { getLegacyRecords, type LegacyRecord } from "@/lib/api/legacy-client";

/** Read-only relationship tree; all people are created through the unified physical-person form. */
export function FamilyManager() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const [personId, setPersonId] = useState("");
  const people = useQuery({ queryKey: ["relationship-tree-people"], queryFn: () => getLegacyRecords("/api/related-persons/detailed") });
  const tree = useQuery({ queryKey: ["relationship-tree", personId], queryFn: () => getLegacyRecords(`/api/related-persons/${personId}/relationship-tree`), enabled: Boolean(personId) });

  return <section className="mt-10 border-t border-border-subtle pt-8">
    <div className="flex items-start gap-3"><Network className="mt-1 size-6 text-text-brand" /><div><Heading level={2} size={3}>{bs ? "Stablo povezanosti" : "Relationship tree"}</Heading><Text tone="secondary" className="mt-1">{bs ? "Odaberite bilo koje fizičko lice, uključujući zaposlenika, kako biste vidjeli sve njegove porodične i druge evidentirane veze." : "Select any individual, including an employee, to see all recorded family and other relationships."}</Text></div></div>
    <label className="mt-5 grid max-w-lg gap-1.5 text-sm font-medium">{bs ? "Fizičko lice" : "Individual"}<select className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3 text-text-primary" value={personId} onChange={(event) => setPersonId(event.target.value)}><option value="">{bs ? "Odaberite lice…" : "Select an individual…"}</option>{(people.data ?? []).map((person) => <option key={String(person["id"])} value={String(person["id"])}>{String(person["firstName"])} {String(person["lastName"])} — {personType(person, bs)}</option>)}</select></label>
    <div className="mt-5 min-h-28 rounded-sm border border-border-subtle bg-surface-default p-4">
      {!personId ? <Text tone="secondary">{bs ? "Odaberite lice za prikaz stabla." : "Select an individual to display the tree."}</Text> : tree.isLoading ? <Text tone="secondary">{bs ? "Učitavanje stabla…" : "Loading tree…"}</Text> : tree.isError ? <Text className="text-feedback-danger">{bs ? "Stablo nije moguće učitati." : "The tree could not be loaded."}</Text> : (tree.data ?? []).length === 0 ? <Text tone="secondary">{bs ? "Za odabrano lice nema evidentiranih veza." : "No relationships are recorded for this individual."}</Text> : <ul className="space-y-2">{(tree.data ?? []).map((node) => <TreeNode key={String(node["id"])} node={node} bs={bs} />)}</ul>}
    </div>
  </section>;
}

function TreeNode({ node, bs }: { readonly node: LegacyRecord; readonly bs: boolean }) {
  const children = Array.isArray(node["children"]) ? node["children"].filter(isRecord) : [];
  const type = String(node["personType"] ?? "RelatedPerson");
  return <li><div className="flex flex-wrap items-center gap-2 rounded-sm border border-border-subtle bg-surface-subtle px-3 py-2"><UserRound className="size-4" /><strong>{String(node["firstName"])} {String(node["lastName"])}</strong><IconIndicator size="sm" kind={type === "Employee" ? "employee" : type === "FamilyMember" ? "family" : "related"} label={localizedType(type, bs)} />{node["relationshipTypeLabel"] ? <span className="text-xs text-text-secondary">{String(node["relationshipTypeLabel"])}</span> : null}</div>{children.length ? <ul className="ml-5 mt-2 space-y-2 border-l border-border-subtle pl-4">{children.map((child) => <TreeNode key={String(child["id"])} node={child} bs={bs} />)}</ul> : null}</li>;
}

function personType(person: LegacyRecord, bs: boolean) { return localizedType(String(person["personType"] ?? (person["relatedToPersonId"] ? "FamilyMember" : person["isIdentifiedStaff"] ? "Employee" : "RelatedPerson")), bs); }
function localizedType(type: string, bs: boolean) { if (type === "Employee") return bs ? "Zaposlenik" : "Employee"; if (type === "FamilyMember") return bs ? "Član porodice" : "Family member"; return bs ? "Povezano lice" : "Related person"; }
function isRecord(value: unknown): value is LegacyRecord { return Boolean(value) && typeof value === "object" && !Array.isArray(value); }

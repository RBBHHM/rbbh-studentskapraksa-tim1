import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Landmark, Pencil, Plus, X } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import { ExportButton } from "@/components/registry/export-button";
import { Button } from "@/components/ui/button";
import { Heading, Text } from "@/components/ui/typography";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { canWriteApplicationAccess } from "@/lib/auth/application-access";
import { getLegacyRecords, type LegacyRecord } from "@/lib/api/legacy-client";

export function CapitalPage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const [editing, setEditing] = useState<LegacyRecord | null>();
  const canWrite = canWriteApplicationAccess("kapital");
  const query = useQuery({ queryKey: ["capital"], queryFn: () => getLegacyRecords("/api/capital") });
  return <section>
    <div className="flex flex-wrap items-start justify-between gap-4"><div className="flex items-start gap-3"><Landmark className="mt-1 size-7 text-text-brand" /><div><Heading level={1} size={4}>{bs ? "Kapital" : "Capital"}</Heading><Text tone="secondary" className="mt-2">{bs ? "Zajednički kapital banke vrijedi za sve limite prema datumu." : "Bank-wide capital applies to all limits by date."}</Text></div></div><div className="flex gap-2"><ExportButton endpoint="/api/capital/export" fileName="kapital.xlsx" />{canWrite && <Button onClick={() => setEditing(null)}><Plus className="size-4" />{bs ? "Novi zapis" : "New record"}</Button>}</div></div>
    <div className="mt-6 overflow-x-auto rounded-sm border border-border-subtle bg-surface-default"><table className="w-full min-w-[760px] text-left text-sm"><thead className="bg-surface-subtle"><tr><th className="px-4 py-3">{bs ? "Datum kapitala" : "Capital date"}</th><th className="px-4 py-3">{bs ? "Osnovni kapital" : "Core capital"}</th><th className="px-4 py-3">{bs ? "Regulatorni kapital" : "Regulatory capital"}</th><th className="px-4 py-3">{bs ? "Dopunski kapital" : "Supplementary capital"}</th>{canWrite && <th className="px-4 py-3 text-center">{bs ? "Akcije" : "Actions"}</th>}</tr></thead><tbody className="divide-y divide-border-subtle">{(query.data ?? []).map((item) => <tr key={String(item["id"])}><td className="px-4 py-3 font-medium">{formatDate(item["datumKapitala"], bs)}</td><td className="px-4 py-3">{currency(item["osnovniKapital"], bs)}</td><td className="px-4 py-3">{currency(item["regulatorniKapital"], bs)}</td><td className="px-4 py-3">{currency(item["dopunskiKapital"], bs)}</td>{canWrite && <td className="px-4 py-2 text-center"><Button size="icon" variant="ghost" title={bs ? "Uredi kapital" : "Edit capital"} onClick={() => setEditing(item)}><Pencil className="size-4" /></Button></td>}</tr>)}</tbody></table>{query.isLoading && <Text tone="secondary" className="p-5">{bs ? "Učitavanje…" : "Loading…"}</Text>}{query.isError && <Text className="p-5 text-feedback-danger">{bs ? "Kapital nije moguće učitati." : "Capital could not be loaded."}</Text>}</div>
    {canWrite && editing !== undefined && <CapitalEditor record={editing ?? undefined} bs={bs} close={() => setEditing(undefined)} saved={async () => { setEditing(undefined); await cache.invalidateQueries({ queryKey: ["capital"] }); }} />}
  </section>;
}

function CapitalEditor({ record, bs, close, saved }: { readonly record: LegacyRecord | undefined; readonly bs: boolean; readonly close: () => void; readonly saved: () => Promise<void> }) {
  const [regulatory, setRegulatory] = useState(String(record?.["regulatorniKapital"] ?? ""));
  const [core, setCore] = useState(String(record?.["osnovniKapital"] ?? ""));
  const [supplementary, setSupplementary] = useState(String(record?.["dopunskiKapital"] ?? ""));
  const [capitalDate, setCapitalDate] = useState(String(record?.["datumKapitala"] ?? "").slice(0, 10));
  const [error, setError] = useState("");
  const mutation = useMutation({ mutationFn: () => { const body = { regulatorniKapital: Number(regulatory), osnovniKapital: Number(core), dopunskiKapital: Number(supplementary), datumKapitala: capitalDate }; return record ? apiClient.putLegacy(`/api/capital/${String(record["id"])}`, { body }) : apiClient.postLegacy("/api/capital", { body }); }, onSuccess: async () => { toast.success(bs ? "Kapital je sačuvan." : "Capital saved."); await saved(); }, onError: (reason) => toast.error(apiErrorMessage(reason, bs ? "Kapital nije sačuvan." : "Capital was not saved.")) });
  const invalid = [regulatory, core, supplementary].some((value) => value === "" || Number(value) < 0) || !capitalDate;
  return <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}><form className="w-full max-w-3xl rounded-sm border border-border-subtle bg-surface-raised p-6 shadow-2xl" onSubmit={(event) => { event.preventDefault(); if (invalid) { setError(bs ? "Sva četiri polja su obavezna, a iznosi ne mogu biti negativni." : "All four fields are required and amounts cannot be negative."); return; } setError(""); mutation.mutate(); }}><div className="flex items-center justify-between"><Heading level={2} size={3}>{record ? (bs ? "Uredi kapital" : "Edit capital") : (bs ? "Novi kapital" : "New capital")}</Heading><Button type="button" variant="ghost" size="icon" onClick={close}><X className="size-5" /></Button></div><div className="mt-5 grid gap-4 sm:grid-cols-2"><CapitalInput label={bs ? "Osnovni kapital" : "Core capital"} value={core} set={setCore} /><CapitalInput label={bs ? "Regulatorni kapital" : "Regulatory capital"} value={regulatory} set={setRegulatory} /><CapitalInput label={bs ? "Dopunski kapital" : "Supplementary capital"} value={supplementary} set={setSupplementary} /><label className="grid gap-1.5 text-sm font-medium">{bs ? "Datum kapitala" : "Capital date"} *<input required type="date" value={capitalDate} onChange={(event) => setCapitalDate(event.target.value)} className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3" /></label></div>{error && <p className="mt-4 text-sm font-medium text-feedback-danger">{error}</p>}<div className="mt-6 flex justify-end gap-2"><Button type="button" variant="secondary" onClick={close}>{bs ? "Odustani" : "Cancel"}</Button><Button type="submit" disabled={mutation.isPending}>{bs ? "Sačuvaj" : "Save"}</Button></div></form></div>;
}
function CapitalInput({ label, value, set }: { readonly label: string; readonly value: string; readonly set: (value: string) => void }) { return <label className="grid gap-1.5 text-sm font-medium">{label} *<input required type="number" min="0" step="0.01" value={value} onChange={(event) => set(event.target.value)} className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3" /></label>; }
function currency(value: unknown, bs: boolean) { return new Intl.NumberFormat(bs ? "bs-BA" : "en-GB", { style: "currency", currency: "BAM" }).format(Number(value ?? 0)); }
function formatDate(value: unknown, bs: boolean) { const date = new Date(String(value ?? "")); return Number.isNaN(date.valueOf()) ? "—" : new Intl.DateTimeFormat(bs ? "bs-BA" : "en-GB").format(date); }

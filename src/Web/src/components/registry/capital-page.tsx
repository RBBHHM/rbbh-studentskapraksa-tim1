import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Landmark, Pencil, X } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Heading, Text } from "@/components/ui/typography";
import { ExportButton } from "@/components/registry/export-button";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { canWriteApplicationAccess } from "@/lib/auth/application-access";

interface CapitalRecord {
  readonly id: string;
  readonly regulatoryCapital: number;
  readonly coreCapital: number;
  readonly supplementaryCapital: number;
  readonly capitalDate: string;
}

export function CapitalPage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const [editing, setEditing] = useState(false);
  const canWrite = canWriteApplicationAccess("kapital");
  const query = useQuery({
    queryKey: ["capital"],
    queryFn: () => apiClient.getLegacy<CapitalRecord | null>("/api/capital"),
  });
  const invalidate = async () => cache.invalidateQueries({ queryKey: ["capital"] });
  const capital = query.data ?? undefined;
  return (
    <section>
      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div className="flex items-start gap-3">
          <Landmark className="mt-1 size-7 text-text-brand" />
          <div>
            <Heading level={1} size={4}>{bs ? "Kapital" : "Capital"}</Heading>
            <Text tone="secondary" className="mt-2 max-w-prose">
              {bs
                ? "Regulatorni, osnovni i dopunski kapital banke, korišten za auto-popunjavanje limita."
                : "The bank's regulatory, core and supplementary capital, used to auto-fill limits."}
            </Text>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <ExportButton endpoint="/api/capital/export" fileName={`kapital-${new Date().toISOString().slice(0, 10)}.xlsx`} />
          {canWrite && (
            <Button onClick={() => setEditing(true)}>
              <Pencil className="size-4" />
              {bs ? "Uredi kapital" : "Edit capital"}
            </Button>
          )}
        </div>
      </div>
      <div className="mt-6 overflow-hidden rounded-sm border border-border-subtle bg-surface-default">
        <table className="w-full min-w-[520px] text-left text-sm">
          <thead className="bg-surface-subtle">
            <tr>
              <th className="px-4 py-3">{bs ? "Osnovni kapital" : "Core capital"}</th>
              <th className="px-4 py-3">{bs ? "Regulatorni kapital" : "Regulatory capital"}</th>
              <th className="px-4 py-3">{bs ? "Dopunski kapital" : "Supplementary capital"}</th>
              <th className="px-4 py-3">{bs ? "Datum kapitala" : "Capital date"}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {capital ? (
              <tr>
                <td className="px-4 py-3">{currency(capital.coreCapital, bs)}</td>
                <td className="px-4 py-3">{currency(capital.regulatoryCapital, bs)}</td>
                <td className="px-4 py-3">{currency(capital.supplementaryCapital, bs)}</td>
                <td className="px-4 py-3">{formatDate(capital.capitalDate, bs)}</td>
              </tr>
            ) : null}
          </tbody>
        </table>
        {query.isLoading ? <Text tone="secondary" className="p-5">{bs ? "Učitavanje…" : "Loading…"}</Text> : null}
        {query.isError ? <Text className="p-5 text-feedback-danger">{bs ? "Kapital nije moguće učitati." : "Capital could not be loaded."}</Text> : null}
        {query.isSuccess && !capital ? (
          <Text tone="secondary" className="p-5">{bs ? "Kapital nije postavljen." : "Capital has not been set yet."}</Text>
        ) : null}
      </div>
      {canWrite && editing ? (
        <CapitalEditor
          regulatoryCapital={capital?.regulatoryCapital}
          coreCapital={capital?.coreCapital}
          supplementaryCapital={capital?.supplementaryCapital}
          capitalDate={capital?.capitalDate}
          bs={bs}
          close={() => setEditing(false)}
          saved={async () => { setEditing(false); await invalidate(); }}
        />
      ) : null}
    </section>
  );
}

function CapitalEditor({
  regulatoryCapital,
  coreCapital,
  supplementaryCapital,
  capitalDate,
  bs,
  close,
  saved,
}: {
  readonly regulatoryCapital?: number | undefined;
  readonly coreCapital?: number | undefined;
  readonly supplementaryCapital?: number | undefined;
  readonly capitalDate?: string | undefined;
  readonly bs: boolean;
  readonly close: () => void;
  readonly saved: () => Promise<void>;
}) {
  const [regulatory, setRegulatory] = useState(String(regulatoryCapital ?? ""));
  const [core, setCore] = useState(String(coreCapital ?? ""));
  const [supplementary, setSupplementary] = useState(String(supplementaryCapital ?? ""));
  const [date, setDate] = useState(toDateInputValue(capitalDate));
  const [error, setError] = useState("");
  const mutation = useMutation({
    mutationFn: () =>
      apiClient.putLegacy("/api/capital", {
        body: { regulatoryCapital: Number(regulatory), coreCapital: Number(core), supplementaryCapital: Number(supplementary), capitalDate: date },
      }),
    onSuccess: async () => { toast.success(bs ? "Kapital je sačuvan." : "Capital saved."); await saved(); },
    onError: (reason) => toast.error(apiErrorMessage(reason, bs ? "Kapital nije sačuvan." : "Capital was not saved.")),
  });
  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}>
      <form
        className="w-full max-w-2xl rounded-sm border border-border-subtle bg-surface-raised p-6 shadow-2xl"
        onSubmit={(event) => {
          event.preventDefault();
          if (!regulatory || !core || !supplementary || Number(regulatory) < 0 || Number(core) < 0 || Number(supplementary) < 0) {
            setError(bs ? "Sva tri iznosa su obavezna i ne mogu biti negativna." : "All three amounts are required and cannot be negative.");
            return;
          }
          if (!date) {
            setError(bs ? "Datum kapitala je obavezan." : "Capital date is required.");
            return;
          }
          setError("");
          mutation.mutate();
        }}
      >
        <div className="flex items-center justify-between">
          <Heading level={2} size={3}>{bs ? "Uredi kapital" : "Edit capital"}</Heading>
          <Button type="button" variant="ghost" size="icon" onClick={close}><X className="size-5" /></Button>
        </div>
        <div className="mt-5 grid gap-4 sm:grid-cols-2">
          <CapitalInput label={bs ? "Regulatorni kapital" : "Regulatory capital"} value={regulatory} set={setRegulatory} />
          <CapitalInput label={bs ? "Osnovni kapital" : "Core capital"} value={core} set={setCore} />
          <CapitalInput label={bs ? "Dopunski kapital" : "Supplementary capital"} value={supplementary} set={setSupplementary} />
          <label className="grid gap-1.5 text-sm font-medium">
            {bs ? "Datum kapitala" : "Capital date"} *
            <input required type="date" value={date} onChange={(event) => setDate(event.target.value)} className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3" />
          </label>
        </div>
        {error ? <p className="mt-4 text-sm font-medium text-feedback-danger">{error}</p> : null}
        <div className="mt-6 flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={close}>{bs ? "Odustani" : "Cancel"}</Button>
          <Button type="submit" disabled={mutation.isPending}>{bs ? "Sačuvaj" : "Save"}</Button>
        </div>
      </form>
    </div>
  );
}

function CapitalInput({ label, value, set }: { readonly label: string; readonly value: string; readonly set: (value: string) => void }) { return <label className="grid gap-1.5 text-sm font-medium">{label} *<input required type="number" min="0" step="0.01" value={value} onChange={(event) => set(event.target.value)} className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3" /></label>; }
function currency(value: unknown, bs: boolean) { return new Intl.NumberFormat(bs ? "bs-BA" : "en-GB", { style: "currency", currency: "BAM" }).format(Number(value ?? 0)); }
function toDateInputValue(value: unknown) { const date = new Date(String(value ?? "")); return Number.isNaN(date.getTime()) ? "" : date.toISOString().slice(0, 10); }
function formatDate(value: unknown, bs: boolean) { const date = new Date(String(value ?? "")); return Number.isNaN(date.getTime()) ? "—" : date.toLocaleDateString(bs ? "bs-BA" : "en-GB"); }

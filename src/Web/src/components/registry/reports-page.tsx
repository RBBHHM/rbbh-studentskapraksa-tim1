import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FilePlus2, RefreshCw } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Heading, Text } from "@/components/ui/typography";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { getLegacyRecords } from "@/lib/api/legacy-client";

export function ReportsPage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [identifier, setIdentifier] = useState("");
  const query = useQuery({
    queryKey: ["reports"],
    queryFn: async () => {
      const [daily, monthly] = await Promise.all([
        getLegacyRecords("/api/reports/daily?page=1&pageSize=100"),
        getLegacyRecords("/api/reports/monthly?page=1&pageSize=100"),
      ]);
      return [...daily, ...monthly].sort((a, b) => String(b["createdAt"] ?? b["reportDate"] ?? "").localeCompare(String(a["createdAt"] ?? a["reportDate"] ?? "")));
    },
  });
  const generate = useMutation({
    mutationFn: (kind: "daily" | "monthly") =>
      apiClient.postLegacy(
        kind === "daily" ? "/api/reports/daily" : `/api/reports/monthly/${year}/${month}`,
        { body: {} },
      ),
    onSuccess: async (report) => {
      const id = String((report as Record<string, unknown>)["id"] ?? "");
      toast.success(bs ? "Izvještaj je generisan i preuzimanje je pokrenuto." : "Report generated and download started.");
      await cache.invalidateQueries({ queryKey: ["reports"] });
      if (id) await download(`/api/reports/export/generated/${encodeURIComponent(id)}`, `izvjestaj-${id}.xlsx`, bs);
    },
    onError: (error) =>
      toast.error(apiErrorMessage(error, bs ? "Generisanje izvještaja nije uspjelo." : "Report generation failed.")),
  });
  const records = query.data ?? [];
  const validPeriod = year >= 2020 && year <= now.getFullYear() + 1 && month >= 1 && month <= 12;
  const validIdentifier = /^\d{1,13}$/.test(identifier.trim());
  return (
    <section>
      <Heading level={1} size={4}>
        {bs ? "Regulatorno izvještavanje" : "Regulatory reporting"}
      </Heading>
      <Text tone="secondary" className="mt-2">
        {bs
          ? "Generišite dnevne i mjesečne izvještaje te preuzmite Excel podatke."
          : "Generate daily and monthly reports and download Excel data."}
      </Text>
      <div className="mt-6 flex flex-wrap items-end gap-3 rounded-sm border border-border-subtle bg-surface-default p-4">
        <Button disabled={generate.isPending} onClick={() => generate.mutate("daily")}>
          <FilePlus2 className="size-4" />
          {bs ? "Generiši dnevni" : "Generate daily"}
        </Button>
        <label className="grid gap-1 text-sm">
          <span>{bs ? "Godina" : "Year"}</span>
          <input
            className="h-10 w-28 rounded-sm border border-border-subtle bg-surface-default px-3"
            type="number"
            min="2020"
            max={now.getFullYear() + 1}
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
          />
        </label>
        <label className="grid gap-1 text-sm">
          <span>{bs ? "Mjesec" : "Month"}</span>
          <input
            className="h-10 w-24 rounded-sm border border-border-subtle bg-surface-default px-3"
            type="number"
            min="1"
            max="12"
            value={month}
            onChange={(e) => setMonth(Number(e.target.value))}
          />
        </label>
        <Button disabled={generate.isPending || !validPeriod} onClick={() => generate.mutate("monthly")}>
          <FilePlus2 className="size-4" />
          {bs ? "Generiši mjesečni" : "Generate monthly"}
        </Button>
        <Button
          variant="secondary"
          onClick={() => download("/api/reports/export/all-clients", "svi-klijenti.xlsx", bs)}
        >
          <Download className="size-4" />
          {bs ? "Svi klijenti" : "All clients"}
        </Button>
        <label className="grid min-w-56 flex-1 gap-1 text-sm">
          <span>{bs ? "Porezni broj ili FBA ID" : "Tax number or FBA ID"}</span>
          <input
            className="h-10 rounded-sm border border-border-subtle bg-surface-default px-3"
            inputMode="numeric"
            maxLength={13}
            aria-invalid={identifier.length > 0 && !validIdentifier}
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
          />
          {identifier.length > 0 && !validIdentifier && <span className="text-xs text-text-danger">{bs ? "Unesite najviše 13 cifara." : "Enter up to 13 digits."}</span>}
        </label>
        <Button
          variant="secondary"
          disabled={!validIdentifier}
          onClick={() =>
            download(
              `/api/reports/export/client/${encodeURIComponent(identifier.trim())}`,
              `klijent-${identifier}.xlsx`,
              bs,
            )
          }
        >
          <Download className="size-4" />
          {bs ? "Preuzmi klijenta" : "Download client"}
        </Button>
      </div>
      <div className="mt-5 overflow-x-auto rounded-sm border border-border-subtle bg-surface-default">
        {query.isLoading ? (
          <div className="p-8 text-center">
            <RefreshCw className="mx-auto size-5 animate-spin" />
          </div>
        ) : query.isError ? (
          <div className="p-8 text-center"><p>{bs ? "Izvještaji trenutno nisu dostupni." : "Reports are currently unavailable."}</p><Button variant="secondary" className="mt-4" onClick={() => query.refetch()}>{bs ? "Pokušaj ponovo" : "Try again"}</Button></div>
        ) : records.length === 0 ? (
          <div className="p-8 text-center text-text-secondary">{bs ? "Još nema generisanih izvještaja za prikaz." : "There are no generated reports to display yet."}</div>
        ) : (
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="bg-surface-subtle">
              <tr>
                {[
                  bs ? "Tip" : "Type",
                  bs ? "Datum" : "Date",
                  bs ? "Klijenti" : "Clients",
                  bs ? "Prekoračenja" : "Breaches",
                  bs ? "Izloženost" : "Exposure",
                  bs ? "Kreirao" : "Created by",
                  bs ? "Datoteka" : "File",
                ].map((x) => (
                  <th className="px-4 py-3" key={x}>
                    {x}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {records.map((r, i) => (
                <tr key={String(r["id"] ?? i)}>
                  <td className="px-4 py-3">{reportTypeLabel(String(r["reportType"] ?? ""), bs)}</td>
                  <td className="px-4 py-3">{String(r["reportDate"] ?? "—")}</td>
                  <td className="px-4 py-3">{String(r["totalClients"] ?? 0)}</td>
                  <td className="px-4 py-3">{String(r["clientsWithBreachedLimit"] ?? 0)}</td>
                  <td className="px-4 py-3">{String(r["totalExposure"] ?? 0)}</td>
                  <td className="px-4 py-3">{String(r["createdBy"] ?? "—")}</td>
                  <td className="px-4 py-3 text-center align-middle"><div className="flex items-center justify-center"><Button size="sm" variant="secondary" onClick={() => download(`/api/reports/export/generated/${encodeURIComponent(String(r["id"]))}`, `izvjestaj-${String(r["id"])}.xlsx`, bs)}><Download className="size-4" />{bs ? "Preuzmi" : "Download"}</Button></div></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </section>
  );
}

function reportTypeLabel(value: string, bs: boolean) {
  const normalized = value.toLowerCase();
  if (normalized === "daily") return bs ? "Dnevni" : "Daily";
  if (normalized === "monthly") return bs ? "Mjesečni" : "Monthly";
  return value || "—";
}

async function download(path: string, fallbackName: string, bs: boolean) {
  try {
    const response = await apiClient.download(path);
    const url = URL.createObjectURL(await response.blob());
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename(response.headers.get("content-disposition")) ?? fallbackName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
  } catch (error) {
    toast.error(apiErrorMessage(error, bs ? "Preuzimanje nije uspjelo." : "Download failed."));
  }
}
function filename(disposition: string | null) {
  return disposition?.match(/filename\*?=(?:UTF-8'')?["']?([^"';]+)/i)?.[1]
    ? decodeURIComponent(disposition.match(/filename\*?=(?:UTF-8'')?["']?([^"';]+)/i)![1]!)
    : undefined;
}

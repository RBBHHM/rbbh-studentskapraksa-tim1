import { type ChangeEvent, useState } from "react";
import { FileUp } from "lucide-react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";

type ImportResult = { readonly imported?: number; readonly failed?: number; readonly errors?: readonly string[] };

export function ImportPanel({ endpoint, onDone }: { endpoint: string; onDone?: () => void }) {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const [result, setResult] = useState<ImportResult>();
  const [uploading, setUploading] = useState(false);
  const upload = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    if (file.size > 10 * 1024 * 1024) { toast.error(bs ? "Datoteka smije imati najviše 10 MB." : "File must not exceed 10 MB."); event.target.value = ""; return; }
    if (!file.name.toLowerCase().endsWith(".xlsx")) { toast.error(bs ? "Dozvoljena je samo Excel .xlsx datoteka." : "Only an Excel .xlsx file is allowed."); event.target.value = ""; return; }
    if (file.size === 0) { toast.error(bs ? "Datoteka je prazna." : "The file is empty."); event.target.value = ""; return; }
    const form = new FormData();
    form.append("file", file);
    try {
      setUploading(true);
      const importResult = await apiClient.postLegacy<ImportResult>(endpoint, { body: form });
      setResult(importResult);
      if ((importResult.failed ?? 0) === 0) toast.success(bs ? `Uspješno je uvezeno ${importResult.imported ?? 0} zapisa.` : `${importResult.imported ?? 0} records imported successfully.`);
      onDone?.();
      window.dispatchEvent(new CustomEvent("registry:data-changed"));
    } catch (error) { toast.error(apiErrorMessage(error, bs ? "Uvoz nije uspio." : "Import failed.")); }
    finally { setUploading(false); event.target.value = ""; }
  };
  return <>
    <label className="inline-flex h-10 cursor-pointer items-center gap-2 rounded-sm border-2 border-[var(--action-secondary-border)] bg-surface-default px-4 text-sm font-semibold text-[var(--action-secondary-foreground)] transition-colors hover:bg-[var(--action-secondary-background-hover)]">
      <FileUp className="size-4" />{uploading ? (bs ? "Provjera…" : "Checking…") : bs ? "Uvezi Excel" : "Import Excel"}
      <input className="sr-only" type="file" accept=".xlsx" disabled={uploading} onChange={upload} />
    </label>
    <Dialog open={Boolean(result)} onOpenChange={(open) => !open && setResult(undefined)}>
      <DialogContent className="max-w-2xl">
        <DialogHeader><DialogTitle>{bs ? "Rezultat Excel uvoza" : "Excel import result"}</DialogTitle><DialogDescription>{bs ? `Uvezeno: ${result?.imported ?? 0}. Neuspješno: ${result?.failed ?? 0}.` : `Imported: ${result?.imported ?? 0}. Failed: ${result?.failed ?? 0}.`}</DialogDescription></DialogHeader>
        {(result?.errors?.length ?? 0) > 0 ? <div className="max-h-[50vh] overflow-y-auto rounded-sm border border-feedback-danger/40 bg-feedback-danger/10 p-4"><p className="font-semibold text-feedback-danger">{bs ? "Šta je potrebno ispraviti" : "What needs to be corrected"}</p><ol className="mt-3 list-decimal space-y-2 pl-5 text-sm text-text-primary">{result?.errors?.map((error, index) => <li key={`${index}-${error}`}>{error}</li>)}</ol></div> : <div className="rounded-sm border border-feedback-success/40 bg-feedback-success/10 p-4 text-sm text-feedback-success">{bs ? "Svi redovi su uspješno obrađeni." : "All rows were processed successfully."}</div>}
        <DialogFooter><Button onClick={() => setResult(undefined)}>{bs ? "Zatvori" : "Close"}</Button></DialogFooter>
      </DialogContent>
    </Dialog>
  </>;
}

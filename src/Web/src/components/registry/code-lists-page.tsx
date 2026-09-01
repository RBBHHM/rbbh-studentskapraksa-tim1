import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileUp, Pencil, Plus, Trash2 } from "lucide-react";
import { type ChangeEvent, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { IconIndicator } from "@/components/registry/icon-indicator";
import { Heading, Text } from "@/components/ui/typography";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { getLegacyRecords, type LegacyRecord } from "@/lib/api/legacy-client";

export function CodeListsPage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const categories = useQuery({
    queryKey: ["code-list-categories"],
    queryFn: async () => {
      const raw = await apiClient.getLegacy<unknown>("/api/code-lists");
      if (Array.isArray(raw)) return raw.map(String);
      if (raw && typeof raw === "object") {
        const a = Object.values(raw).find(Array.isArray);
        return Array.isArray(a) ? a.map(String) : [];
      }
      return [];
    },
  });
  const [category, setCategory] = useState("");
  useEffect(() => {
    if (!category && categories.data?.[0]) setCategory(categories.data[0]);
  }, [categories.data, category]);
  const items = useQuery({
    queryKey: ["code-list", category],
    queryFn: () => getLegacyRecords(`/api/code-lists/${encodeURIComponent(category)}`),
    enabled: Boolean(category),
  });
  const [edit, setEdit] = useState<LegacyRecord | null | undefined>(undefined);
  const [deleteId, setDeleteId] = useState<string>();
  const [deleteCategoryOpen, setDeleteCategoryOpen] = useState(false);
  const [pendingImport, setPendingImport] = useState<{ form: FormData; total: string; ready: number; duplicates: number; errors: string[] }>();
  const [definitionOpen, setDefinitionOpen] = useState(false);
  const refresh = () => cache.invalidateQueries({ queryKey: ["code-list", category] });
  const remove = useMutation({
    mutationFn: (id: string) => apiClient.deleteLegacy(`/api/code-lists/${id}`),
    onSuccess: async () => {
      toast.success(bs ? "Vrijednost je obrisana." : "Value deleted.");
      await refresh();
    },
    onError: (error) =>
      toast.error(apiErrorMessage(error,
        bs
          ? "Vrijednost je u upotrebi ili je brisanje odbijeno."
          : "The value is in use or deletion was rejected.",
      )),
  });
  const removeCategory = useMutation({
    mutationFn: () => apiClient.deleteLegacy(`/api/code-lists/categories/${encodeURIComponent(category)}`),
    onSuccess: async () => {
      const removedCategory = category;
      const remainingCategories = (categories.data ?? []).filter(
        (item) => item.toLocaleLowerCase() !== removedCategory.toLocaleLowerCase(),
      );
      cache.setQueryData(["code-list-categories"], remainingCategories);
      cache.removeQueries({ queryKey: ["code-list", removedCategory] });
      setCategory(remainingCategories[0] ?? "");
      toast.success(bs ? "Šifrarnik i njegove vrijednosti su obrisani." : "The code list and its values were deleted.");
      await cache.invalidateQueries({ queryKey: ["code-list-categories"] });
    },
    onError: (error) =>
      toast.error(
        apiErrorMessage(
          error,
          bs
            ? "Šifrarnik nije moguće obrisati jer je neka njegova vrijednost u upotrebi."
            : "The code list cannot be deleted because one of its values is in use.",
        ),
      ),
  });
  const importFile = async (e: ChangeEvent<HTMLInputElement>, dryRun: boolean) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size === 0) {
      toast.error(bs ? "Odabrana datoteka je prazna." : "The selected file is empty.");
      e.target.value = "";
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      toast.error(bs ? "Datoteka ne smije biti veća od 10 MB." : "The file must not exceed 10 MB.");
      e.target.value = "";
      return;
    }
    if (!file.name.toLowerCase().endsWith(".xlsx")) {
      toast.error(bs ? "Dozvoljena je samo Excel .xlsx datoteka." : "Only an Excel .xlsx file is allowed.");
      e.target.value = "";
      return;
    }
    const form = new FormData();
    form.append("file", file);
    try {
      const result = await apiClient.postLegacy<Record<string, unknown>>(
        `/api/code-lists/${encodeURIComponent(category)}/import?dryRun=${dryRun}`,
        { body: form },
      );
      const total = String(result["totalRows"] ?? 0),
        imported = String(result["imported"] ?? 0);
      if (dryRun) {
        setPendingImport({ form, total, ready: Array.isArray(result["toImport"]) ? result["toImport"].length : 0, duplicates: Array.isArray(result["duplicates"]) ? result["duplicates"].length : 0, errors: Array.isArray(result["errors"]) ? result["errors"].map(String) : [] });
      } else await refresh();
      toast.success(bs ? `Uvezeno: ${imported}` : `Imported: ${imported}`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : bs ? "Uvoz nije uspio." : "Import failed.");
    } finally {
      e.target.value = "";
    }
  };
  return (
    <section>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <Heading level={1} size={4}>
            {bs ? "Šifrarnici" : "Code lists"}
          </Heading>
          <Text tone="secondary" className="mt-2">
            {bs
              ? "Centralno upravljanje vrijednostima koje aplikacija koristi u formama."
              : "Centrally manage values used by application forms."}
          </Text>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => setDefinitionOpen(true)}><Plus className="size-4" />{bs ? "Novi šifrarnik" : "New code list"}</Button>
          <label className="inline-flex cursor-pointer items-center gap-2 rounded-sm border border-border-subtle px-4 py-2 text-sm font-semibold">
            <FileUp className="size-4" />
            {bs ? "Excel preview i uvoz" : "Excel preview and import"}
            <input
              type="file"
              accept=".xlsx"
              className="sr-only"
              onChange={(e) => importFile(e, true)}
            />
          </label>
          <Button onClick={() => setEdit(null)} disabled={!category}>
            <Plus className="size-4" />
            {bs ? "Nova vrijednost" : "New value"}
          </Button>
        </div>
      </div>
      <div className="mt-6 flex max-w-xl items-end gap-2">
        <label className="grid min-w-0 flex-1 gap-1 text-sm font-medium">
          {bs ? "Kategorija" : "Category"}
          <select
            className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          >
            {(categories.data ?? []).map((c) => (
              <option key={c}>{c}</option>
            ))}
          </select>
        </label>
        <Button
          variant="destructive"
          aria-label={bs ? "Obriši cijeli šifrarnik" : "Delete entire code list"}
          disabled={!category || removeCategory.isPending}
          onClick={() => setDeleteCategoryOpen(true)}
        >
          <Trash2 className="size-4" />
          {bs ? "Obriši šifrarnik" : "Delete code list"}
        </Button>
      </div>
      <div className="mt-5 overflow-x-auto rounded-sm border border-border-subtle bg-surface-default">
        <table className="w-full min-w-[700px] text-left text-sm">
          <thead className="bg-surface-subtle">
            <tr>
              {[
                bs ? "Kod" : "Code",
                bs ? "Naziv" : "Name",
                bs ? "Opis" : "Description",
                bs ? "Aktivan" : "Active",
                bs ? "Akcije" : "Actions",
              ].map((x) => (
                <th key={x} className="px-4 py-3">
                  {x}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {(items.data ?? []).map((r, i) => {
              const id = String(r["id"] ?? r["ID"] ?? i);
              const active = readBoolean(r["aktivan"] ?? r["Aktivan"] ?? r["isActive"] ?? true);
              return (
                <tr key={id} className={active ? undefined : "bg-surface-subtle/60"}>
                  <td className="px-4 py-3">{String(r["code"] ?? r["kod"] ?? r["Kod"] ?? "—")}</td>
                  <td className="px-4 py-3 font-semibold">
                    {String(r["naziv"] ?? r["Naziv"] ?? r["value"] ?? "—")}
                  </td>
                  <td className="px-4 py-3">{String(r["description"] ?? r["opis"] ?? r["Opis"] ?? "—")}</td>
                  <td className="px-4 py-3 text-center align-middle">
                    {active
                      ? <IconIndicator kind="active" label={bs ? "Aktivan" : "Active"} />
                      : <IconIndicator kind="inactive" label={bs ? "Neaktivan" : "Inactive"} />}
                  </td>
                  <td className="px-4 py-2 text-center align-middle">
                    <div className="flex items-center justify-center gap-1">
                      <Button size="icon" variant="ghost" onClick={() => setEdit(r)}>
                        <Pencil className="size-4" />
                      </Button>
                      <Button
                        size="icon"
                        variant="ghost"
                        onClick={() => setDeleteId(id)}
                      >
                        <Trash2 className="size-4 text-feedback-danger" />
                      </Button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {edit !== undefined && (
        <CodeForm
          bs={bs}
          category={category}
          record={edit}
          close={() => setEdit(undefined)}
          saved={refresh}
        />
      )}
      {definitionOpen && <DefinitionForm bs={bs} close={() => setDefinitionOpen(false)} saved={async (name) => { await cache.invalidateQueries({ queryKey: ["code-list-categories"] }); setCategory(name); }} />}
      <ConfirmDialog open={Boolean(deleteId)} title={bs ? "Obrisati vrijednost?" : "Delete value?"} description={bs ? "Vrijednost će biti uklonjena ako nije u upotrebi." : "The value will be removed if it is not in use."} cancelLabel={bs ? "Odustani" : "Cancel"} confirmLabel={bs ? "Obriši" : "Delete"} destructive onCancel={() => setDeleteId(undefined)} onConfirm={() => { if (deleteId) remove.mutate(deleteId); setDeleteId(undefined); }} />
      <ConfirmDialog open={deleteCategoryOpen} title={bs ? `Obrisati cijeli šifrarnik „${category}“?` : `Delete the entire “${category}” code list?`} description={bs ? "Definicija i sve njene vrijednosti bit će uklonjene. Brisanje neće biti dozvoljeno ako je bilo koja vrijednost u upotrebi." : "The definition and all its values will be removed. Deletion will be blocked if any value is in use."} cancelLabel={bs ? "Odustani" : "Cancel"} confirmLabel={bs ? "Obriši šifrarnik" : "Delete code list"} destructive onCancel={() => setDeleteCategoryOpen(false)} onConfirm={() => { setDeleteCategoryOpen(false); removeCategory.mutate(); }} />
      <ConfirmDialog open={Boolean(pendingImport)} title={bs ? "Potvrditi Excel uvoz?" : "Confirm Excel import?"} description={`${bs ? `Provjereno je ${pendingImport?.total ?? 0} redova: ${pendingImport?.ready ?? 0} spremno za uvoz, ${pendingImport?.duplicates ?? 0} postojećih vrijednosti koje će biti preskočene.` : `${pendingImport?.total ?? 0} rows checked: ${pendingImport?.ready ?? 0} ready to import and ${pendingImport?.duplicates ?? 0} existing values that will be skipped.`}${pendingImport?.errors.length ? ` ${bs ? "Greške" : "Errors"}: ${pendingImport.errors.join(" ")}` : ""}`} cancelLabel={bs ? "Odustani" : "Cancel"} confirmLabel={bs ? "Uvezi ispravne redove" : "Import valid rows"} onCancel={() => setPendingImport(undefined)} onConfirm={async () => { const pending = pendingImport; setPendingImport(undefined); if (!pending) return; try { await apiClient.postLegacy(`/api/code-lists/${encodeURIComponent(category)}/import?dryRun=false`, { body: pending.form }); await refresh(); toast.success(bs ? "Uvoz je završen." : "Import completed."); } catch (error) { toast.error(error instanceof Error ? error.message : bs ? "Uvoz nije uspio." : "Import failed."); } }} />
    </section>
  );
}
function CodeForm({
  bs,
  category,
  record,
  close,
  saved,
}: {
  bs: boolean;
  category: string;
  record: LegacyRecord | null;
  close: () => void;
  saved: () => Promise<unknown>;
}) {
  const id = String(record?.["id"] ?? record?.["ID"] ?? "");
  const [value, setValue] = useState(
    String(record?.["naziv"] ?? record?.["Naziv"] ?? record?.["value"] ?? ""),
  );
  const [description, setDescription] = useState(
    String(record?.["description"] ?? record?.["opis"] ?? record?.["Opis"] ?? ""),
  );
  const [code, setCode] = useState(String(record?.["code"] ?? record?.["kod"] ?? record?.["Kod"] ?? ""));
  const [order, setOrder] = useState(String(record?.["displayOrder"] ?? record?.["redoslijedPrikaza"] ?? record?.["RedoslijedPrikaza"] ?? ""));
  const [active, setActive] = useState(
    Boolean(record?.["aktivan"] ?? record?.["Aktivan"] ?? record?.["isActive"] ?? true),
  );
  const m = useMutation({
    mutationFn: () =>
      record
        ? apiClient.putLegacy(`/api/code-lists/${id}`, {
            body: {
              naziv: value,
              opis: description || null,
              redoslijedPrikaza: order ? Number(order) : null,
              aktivan: active,
            },
          })
        : apiClient.postLegacy("/api/code-lists", { body: { kategorija: category, kod: code, naziv: value, opis: description || null, redoslijedPrikaza: order ? Number(order) : null, aktivan: active } }),
    onSuccess: async () => {
      toast.success(bs ? "Sačuvano." : "Saved.");
      await saved();
      close();
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Čuvanje nije uspjelo." : "Save failed.")),
  });
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}>
      <form
        className="w-full max-w-2xl rounded-sm border border-border-subtle bg-surface-raised p-6 text-text-primary shadow-2xl"
        onSubmit={(e) => {
          e.preventDefault();
          m.mutate();
        }}
      >
        <Heading level={2} size={3}>
          {record ? (bs ? "Uredi vrijednost" : "Edit value") : bs ? "Nova vrijednost" : "New value"}
        </Heading>
        <label className="mt-5 grid gap-1 text-sm font-medium">
          {bs ? "Naziv" : "Name"}
          <input
            required
            className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3"
            value={value}
            onChange={(e) => setValue(e.target.value)}
          />
        </label>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          <label className="grid gap-1 text-sm font-medium">{bs ? "Kod" : "Code"}<input required={!record} disabled={Boolean(record)} maxLength={50} className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3 disabled:bg-surface-muted" value={code} onChange={(e) => setCode(e.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g, ""))} /></label>
          <label className="grid gap-1 text-sm font-medium">{bs ? "Redoslijed prikaza" : "Display order"}<input type="number" min="0" className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3" value={order} onChange={(e) => setOrder(e.target.value)} /></label>
        </div>
            <label className="mt-4 grid gap-1 text-sm font-medium">
              {bs ? "Opis" : "Description"}
              <textarea
                className="min-h-24 rounded-sm border border-border-subtle bg-surface-default p-3"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </label>
        <div className="mt-4">
            <label className="flex min-h-11 items-center gap-2 rounded-sm border border-border-subtle p-3 text-sm font-medium">
              <input
                type="checkbox"
                checked={active}
                onChange={(e) => setActive(e.target.checked)}
              />
              {bs ? "Aktivan" : "Active"}
            </label>
        </div>
        <div className="mt-6 flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={close}>
            {bs ? "Odustani" : "Cancel"}
          </Button>
          <Button type="submit" disabled={m.isPending}>
            {bs ? "Sačuvaj" : "Save"}
          </Button>
        </div>
      </form>
    </div>
  );
}

function readBoolean(value: unknown): boolean {
  if (typeof value === "boolean") return value;
  if (typeof value === "number") return value === 1;
  if (typeof value === "string") return !["false", "0", "no", "ne", "inactive", "neaktivan"].includes(value.trim().toLowerCase());
  return Boolean(value);
}

function DefinitionForm({ bs, close, saved }: { bs: boolean; close: () => void; saved: (name: string) => Promise<unknown> }) {
  const [name, setName] = useState(""); const [description, setDescription] = useState("");
  const mutation = useMutation({ mutationFn: () => apiClient.postLegacy("/api/code-lists/categories", { body: { name, description: description || null } }), onSuccess: async () => { await saved(name.trim()); toast.success(bs ? "Definicija šifrarnika je kreirana." : "Code-list definition created."); close(); }, onError: (error) => toast.error(apiErrorMessage(error, bs ? "Kreiranje šifrarnika nije uspjelo." : "Creating the code list failed.")) });
  return <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}><form className="w-full max-w-xl rounded-sm border border-border-subtle bg-surface-raised p-6 text-text-primary shadow-2xl" onSubmit={(event) => { event.preventDefault(); mutation.mutate(); }}><Heading level={2} size={3}>{bs ? "Novi šifrarnik" : "New code list"}</Heading><label className="mt-5 grid gap-1 text-sm font-medium">{bs ? "Naziv definicije" : "Definition name"}<input required minLength={2} maxLength={100} className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3" value={name} onChange={(e) => setName(e.target.value)} /></label><label className="mt-4 grid gap-1 text-sm font-medium">{bs ? "Poslovna svrha" : "Business purpose"}<textarea maxLength={500} className="min-h-24 rounded-sm border border-border-subtle bg-surface-default p-3" value={description} onChange={(e) => setDescription(e.target.value)} /></label><div className="mt-6 flex justify-end gap-2"><Button variant="secondary" onClick={close}>{bs ? "Odustani" : "Cancel"}</Button><Button type="submit" disabled={mutation.isPending}>{bs ? "Kreiraj" : "Create"}</Button></div></form></div>;
}

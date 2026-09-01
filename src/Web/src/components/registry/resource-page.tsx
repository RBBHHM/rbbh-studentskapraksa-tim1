import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertCircle,
  CheckCircle2,
  Pencil,
  Plus,
  Search,
  Trash2,
  X,
} from "lucide-react";
import { type FormEvent, type ReactNode, useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Heading, Text } from "@/components/ui/typography";
import { IconIndicator, type IndicatorKind } from "@/components/registry/icon-indicator";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { getLegacyRecords, type LegacyRecord } from "@/lib/api/legacy-client";
import type { RegistryResource, ResourceField } from "@/lib/registry/resources";

type EditorState = { readonly mode: "create" | "edit"; readonly record?: LegacyRecord } | null;
const EMPTY_RECORDS: readonly LegacyRecord[] = [];

export function ResourcePage({ resource, toolbar }: { readonly resource: RegistryResource; readonly toolbar?: ReactNode }) {
  const { t, i18n } = useTranslation("registry");
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const [search, setSearch] = useState("");
  const [editor, setEditor] = useState<EditorState>(null);
  const [deleteId, setDeleteId] = useState<string>();
  const queryKey = useMemo(() => ["registry", resource.key] as const, [resource.key]);
  const query = useQuery({
    queryKey,
    queryFn: () => getLegacyRecords(resource.endpoint!),
    enabled: Boolean(resource.endpoint),
  });
  useEffect(() => {
    const sync = () => void cache.invalidateQueries({ queryKey });
    window.addEventListener("registry:data-changed", sync);
    return () => window.removeEventListener("registry:data-changed", sync);
  }, [cache, queryKey]);
  const records = query.data ?? EMPTY_RECORDS;
  const filtered = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return needle
      ? records.filter((record) =>
          Object.values(record).some(
            (value) =>
              isReadable(value) &&
              String(value ?? "")
                .toLocaleLowerCase()
                .includes(needle),
          ),
        )
      : records;
  }, [records, search]);
  const firstRecord = records[0];
  const columns = resource.displayColumns?.filter((key) => firstRecord == null || key in firstRecord) ?? (firstRecord
    ? Object.keys(firstRecord)
        .filter((key) => isReadable(firstRecord[key]))
        .slice(0, 7)
    : (resource.capabilities?.fields.slice(0, 7).map((field) => field.key) ?? []));

  const remove = useMutation({
    mutationFn: (id: string) => {
      const endpoint = resource.capabilities?.mutationEndpoint ?? resource.endpoint;
      return apiClient.deleteLegacy(`${endpoint}/${id}`);
    },
    onSuccess: async () => {
      toast.success(bs ? "Zapis je obrisan." : "Record deleted.");
      await cache.invalidateQueries({ queryKey });
    },
    onError: (error) => toast.error(errorMessage(error, bs)),
  });
  const verify = useMutation({
    mutationFn: (id: string) =>
      apiClient.postLegacy(resource.capabilities!.verifyPath!.replace("{id}", id), { body: {} }),
    onSuccess: async () => {
      toast.success(bs ? "Zapis je verificiran i status je osvježen." : "Record verified and status refreshed.");
      await cache.invalidateQueries({ queryKey });
    },
    onError: (error) => toast.error(errorMessage(error, bs)),
  });

  return (
    <section aria-labelledby={`${resource.key}-heading`} className="min-w-0">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div>
          <p className="text-eyebrow text-text-tertiary">{t(`areas.${resource.area}`)}</p>
          <Heading level={1} size={4} id={`${resource.key}-heading`} className="mt-2">
            {t(`resources.${resource.key}.title` as never)}
          </Heading>
          <Text tone="secondary" className="mt-2 max-w-prose">
            {t(`resources.${resource.key}.description` as never)}
          </Text>
        </div>
        <div className="flex flex-wrap gap-2">
          {toolbar}
          {resource.capabilities?.create && (
            <Button onClick={() => setEditor({ mode: "create" })}>
              <Plus className="size-4" />
              {bs ? "Novi zapis" : "New record"}
            </Button>
          )}
        </div>
      </div>

      <label className="mt-6 flex max-w-md items-center gap-2 rounded-sm border border-border-subtle bg-surface-default px-3 focus-within:border-border-brand">
        <Search className="size-4 shrink-0 text-text-tertiary" />
        <span className="sr-only">{bs ? "Pretraga" : "Search"}</span>
        <input
          className="h-11 w-full bg-transparent text-sm outline-none"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={bs ? "Pretraži sve kolone…" : "Search all columns…"}
        />
      </label>

      <div className="mt-5 overflow-hidden rounded-sm border border-border-subtle bg-surface-default">
        {query.isLoading && <Loading label={t("states.loading")} />}
        {query.isError && <ErrorState retry={() => query.refetch()} bs={bs} />}
        {query.isSuccess && filtered.length === 0 && (
          <div className="px-6 py-16 text-center">
            <h2 className="font-bold">{t("states.emptyTitle")}</h2>
            <Text tone="secondary" className="mt-2">
              {search
                ? bs
                  ? "Nema rezultata za unesenu pretragu."
                  : "No records match your search."
                : t("states.emptyBody")}
            </Text>
          </div>
        )}
        {query.isSuccess && filtered.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] table-auto text-left text-sm">
              <thead className="bg-surface-subtle text-text-secondary">
                <tr>
                  {columns.map((column) => (
                    <th key={column} className="px-4 py-3 font-semibold">
                      {fieldLabel(resource, column, bs)}
                    </th>
                  ))}
                  {resource.capabilities && (
                    <th className="sticky right-0 bg-surface-subtle px-4 py-3 text-center font-semibold">
                      {bs ? "Akcije" : "Actions"}
                    </th>
                  )}
                </tr>
              </thead>
              <tbody className="divide-y divide-border-subtle">
                {filtered.map((record, index) => {
                  const id = String(record["id"] ?? record["Id"] ?? record["ID"] ?? index);
                  return (
                    <tr key={id} className="hover:bg-surface-subtle">
                      {columns.map((column) => (
                        <td
                          key={column}
                          className={`max-w-72 break-words px-4 py-3 align-middle text-text-primary ${isIndicatorValue(record[column], column) ? "text-center" : ""}`}
                        >
                          {isIndicatorValue(record[column], column) ? (
                            <span className="flex items-center justify-center">{formatValue(record[column], bs, column)}</span>
                          ) : formatValue(record[column], bs, column)}
                        </td>
                      ))}
                      {resource.capabilities && (
                        <td className="sticky right-0 bg-surface-default px-4 py-2 text-center align-middle">
                          <div className="flex items-center justify-center gap-1">
                            {resource.capabilities.verifyPath && !isVerified(record) && (
                              <Button
                                variant="ghost"
                                size="icon"
                                title={bs ? "Verificiraj" : "Verify"}
                                onClick={() => verify.mutate(id)}
                              >
                                <CheckCircle2 className="size-4" />
                              </Button>
                            )}
                            {resource.capabilities.update && (
                              <Button
                                variant="ghost"
                                size="icon"
                                title={bs ? "Uredi" : "Edit"}
                                onClick={() => setEditor({ mode: "edit", record })}
                              >
                                <Pencil className="size-4" />
                              </Button>
                            )}
                            {resource.capabilities.remove && (
                              <Button
                                variant="ghost"
                                size="icon"
                                title={bs ? "Obriši" : "Delete"}
                                disabled={remove.isPending}
                                onClick={() => setDeleteId(id)}
                              >
                                <Trash2 className="size-4 text-feedback-danger" />
                              </Button>
                            )}
                          </div>
                        </td>
                      )}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
      {editor && resource.capabilities && (
        <Editor
          resource={resource}
          state={editor}
          bs={bs}
          close={() => setEditor(null)}
          saved={async () => {
            setEditor(null);
            await cache.invalidateQueries({ queryKey });
          }}
        />
      )}
      <ConfirmDialog
        open={Boolean(deleteId)}
        title={bs ? "Obrisati zapis?" : "Delete record?"}
        description={bs ? "Ova radnja može uticati na povezane podatke i ne može se jednostavno poništiti." : "This action may affect related data and cannot be easily undone."}
        cancelLabel={bs ? "Odustani" : "Cancel"}
        confirmLabel={bs ? "Obriši" : "Delete"}
        destructive
        onCancel={() => setDeleteId(undefined)}
        onConfirm={() => { if (deleteId) remove.mutate(deleteId); setDeleteId(undefined); }}
      />
    </section>
  );
}

function Editor({
  resource,
  state,
  bs,
  close,
  saved,
}: {
  resource: RegistryResource;
  state: NonNullable<EditorState>;
  bs: boolean;
  close: () => void;
  saved: () => Promise<void>;
}) {
  const fields = resource.capabilities!.fields;
  const [values, setValues] = useState<Record<string, unknown>>(() =>
    Object.fromEntries(fields.map((field) => [field.key, initialValue(field, state.record)])),
  );
  const [validationError, setValidationError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [step, setStep] = useState<1 | 2>(1);
  const isPhysical = resource.key === "physicalPersons";
  const immediateFamily = isImmediateFamily(values["specialRelationBasis"]);
  const visibleFields = (isPhysical
    ? fields.filter((_, index) => step === 1 ? index < 8 : index >= 8)
    : fields).filter((field) =>
      !isPhysical || immediateFamily || !["relatedToPersonId", "familyRelationshipType"].includes(field.key));
  const mutation = useMutation({
    mutationFn: () => {
      const id = String(state.record?.["id"] ?? state.record?.["Id"] ?? state.record?.["ID"] ?? "");
      const endpoint = resource.capabilities!.mutationEndpoint ?? resource.endpoint!;
      const body = normalizePayload(fields, values);
      return state.mode === "create"
        ? apiClient.postLegacy(endpoint, { body })
        : apiClient.putLegacy(`${endpoint}/${id}`, { body });
    },
    onSuccess: async () => {
      toast.success(bs ? "Promjene su sačuvane." : "Changes saved.");
      await saved();
    },
    onError: (error) => toast.error(errorMessage(error, bs)),
  });
  const checkIdentity = async (field: string, value: unknown) => {
    const normalized = String(value ?? "").trim();
    if (!normalized || !["jmbg", "passportNumber", "fbaId"].includes(field)) {
      setFieldErrors((current) => ({ ...current, [field]: "" }));
      return;
    }
    try {
      const excludeId = String(state.record?.["id"] ?? state.record?.["Id"] ?? "");
      const response = await apiClient.getLegacy<Record<string, unknown>>("/api/related-persons/check-duplicate", { query: { [field]: normalized, excludeId } });
      const result = (response["data"] ?? response["value"] ?? response) as Record<string, unknown>;
      setFieldErrors((current) => ({ ...current, [field]: result["exists"] ? String(result["message"] ?? (bs ? "Ovaj identifikator je već evidentiran." : "This identifier is already recorded.")) : "" }));
    } catch (error) {
      setFieldErrors((current) => ({ ...current, [field]: apiErrorMessage(error, bs ? "Provjera duplikata nije uspjela." : "Duplicate check failed.") }));
    }
  };
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const error = validateResource(resource, values, bs);
    if (error) {
      setValidationError(error);
      return;
    }
    setValidationError("");
    mutation.mutate();
  };
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 p-4"
      role="dialog"
      aria-modal="true"
      onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}
    >
      <form
        onSubmit={submit}
        className="w-full max-w-6xl rounded-sm border border-border-subtle bg-surface-raised p-5 text-text-primary shadow-2xl sm:p-7"
      >
        <div className="flex items-center justify-between gap-4">
          <Heading level={2} size={3}>
            {state.mode === "create"
              ? bs
                ? "Novi zapis"
                : "New record"
              : bs
                ? "Uredi zapis"
                : "Edit record"}
          </Heading>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={close}
            title={bs ? "Zatvori" : "Close"}
          >
            <X className="size-5" />
          </Button>
        </div>
        {isPhysical && <div className="mt-5 grid grid-cols-2 gap-2" aria-label={bs ? "Koraci forme" : "Form steps"}><div className={`h-1 rounded-full ${step >= 1 ? "bg-surface-brand" : "bg-surface-muted"}`} /><div className={`h-1 rounded-full ${step === 2 ? "bg-surface-brand" : "bg-surface-muted"}`} /><span className="text-xs font-semibold">{bs ? "1. Identitet" : "1. Identity"}</span><span className="text-xs font-semibold">{bs ? "2. Povezanost i period" : "2. Relationship and period"}</span></div>}
        <div className="mt-6 grid gap-4 sm:grid-cols-2">
          {visibleFields.map((field) => (
            <Field
              key={field.key}
              field={field}
              value={values[field.key]}
              bs={bs}
              disabled={isConditionallyDisabled(resource.key, field.key, values)}
              error={fieldErrors[field.key] ?? ""}
              onBlur={() => { if (isPhysical) void checkIdentity(field.key, values[field.key]); }}
              set={(value) => { setFieldErrors((current) => ({ ...current, [field.key]: "" })); setValues((current) => updateFieldValues(resource.key, field, value, current, bs)); }}
            />
          ))}
        </div>
        {validationError && (
          <div role="alert" className="mt-5 flex items-start gap-2 rounded-sm border border-feedback-danger bg-feedback-danger/10 p-3 text-sm text-feedback-danger">
            <AlertCircle className="mt-0.5 size-4 shrink-0" />
            <span>{validationError}</span>
          </div>
        )}
        <div className="mt-7 flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={close}>
            {bs ? "Odustani" : "Cancel"}
          </Button>
          {isPhysical && step === 2 && <Button type="button" variant="secondary" onClick={() => setStep(1)}>{bs ? "Nazad" : "Back"}</Button>}
          {isPhysical && step === 1 ? <Button type="button" onClick={() => { const error = validatePhysicalIdentity(values, bs); if (error) setValidationError(error); else if (Object.values(fieldErrors).some(Boolean)) setValidationError(bs ? "Otklonite greške u identifikacionim poljima." : "Resolve the identity field errors."); else { setValidationError(""); setStep(2); } }}>{bs ? "Nastavi" : "Continue"}</Button> : <Button type="submit" disabled={mutation.isPending || Object.values(fieldErrors).some(Boolean)}>{mutation.isPending ? (bs ? "Čuvanje…" : "Saving…") : bs ? "Sačuvaj" : "Save"}</Button>}
        </div>
      </form>
    </div>
  );
}

function Field({
  field,
  value,
  bs,
  set,
  disabled = false,
  error,
  onBlur,
}: {
  field: ResourceField;
  value: unknown;
  bs: boolean;
  set: (value: unknown) => void;
  disabled?: boolean;
  error?: string;
  onBlur?: () => void;
}) {
  const label = bs ? field.labelBs : field.labelEn;
  const codeList = useQuery({
    queryKey: ["field-options", field.codeListCategory, field.lookupEndpoint],
    queryFn: () => getLegacyRecords(field.lookupEndpoint ?? `/api/code-lists/dropdown/${encodeURIComponent(field.codeListCategory!)}`),
    enabled: Boolean(field.codeListCategory || field.lookupEndpoint),
    staleTime: 30_000,
  });
  const dynamicOptions = (codeList.data ?? []).map((item) => ({
    value: field.lookupEndpoint
      ? String(item["id"] ?? item["Id"] ?? "")
      : String(item["kod"] ?? item["Kod"] ?? ""),
    labelBs: field.lookupEndpoint
      ? `${String(item["firstName"] ?? "")} ${String(item["lastName"] ?? "")}`.trim()
      : field.options?.find((option) => String(option.value) === String(item["kod"] ?? item["Kod"] ?? ""))?.labelBs ?? String(item["naziv"] ?? item["Naziv"] ?? ""),
    labelEn: field.lookupEndpoint
      ? `${String(item["firstName"] ?? "")} ${String(item["lastName"] ?? "")}`.trim()
      : field.options?.find((option) => String(option.value) === String(item["kod"] ?? item["Kod"] ?? ""))?.labelEn ?? String(item["naziv"] ?? item["Naziv"] ?? ""),
  })).filter((item) => item.value);
  const options = dynamicOptions.length > 0 ? dynamicOptions : field.options;
  if (field.type === "checkbox")
    return (
      <label className="flex min-h-11 items-center gap-3 rounded-sm border border-border-subtle px-3 text-sm font-medium">
        <input
          type="checkbox"
          checked={Boolean(value)}
          disabled={disabled}
          onChange={(e) => set(e.target.checked)}
          className="size-4"
        />
        {label}
      </label>
    );
  if (field.type === "select")
    return (
      <label className="grid gap-1.5 text-sm font-medium">
        <span>{label}{field.required && " *"}</span>
        <select
          required={field.required}
          disabled={disabled}
          value={String(value ?? "")}
          onChange={(e) => {
            const option = options?.find((item) => String(item.value) === e.target.value);
            const nextValue = option?.value ?? e.target.value;
            set(field.valueKind === "number" ? Number(nextValue) : nextValue);
          }}
          className="h-11 min-w-0 rounded-sm border border-border-subtle bg-surface-default px-3 text-text-primary outline-none focus:border-border-brand"
        >
          <option value="">{bs ? "Odaberite…" : "Select…"}</option>
          {options?.map((option) => <option key={String(option.value)} value={String(option.value)}>{bs ? option.labelBs : option.labelEn}</option>)}
        </select>
      </label>
    );
  if (field.type === "segmented")
    return (
      <fieldset className="grid gap-1.5 text-sm font-medium">
        <legend>{label}{field.required && " *"}</legend>
        <div className="grid min-h-11 grid-cols-2 overflow-hidden rounded-sm border border-border-subtle bg-surface-default p-1">
          {field.options?.map((option) => {
            const selected = String(value ?? "") === String(option.value);
            return (
              <button
                key={String(option.value)}
                type="button"
                aria-pressed={selected}
                disabled={disabled}
                onClick={() => set(option.value)}
                className={`rounded-xs px-3 py-2 text-sm font-semibold transition-colors ${selected ? "bg-surface-brand text-text-on-brand shadow-sm" : "bg-surface-default text-text-secondary hover:bg-surface-muted hover:text-text-primary"}`}
              >
                {bs ? option.labelBs : option.labelEn}
              </button>
            );
          })}
        </div>
      </fieldset>
    );
  if (field.type === "textarea")
    return (
      <label className="grid gap-1.5 text-sm font-medium sm:col-span-2">
        <span>{label}{field.required && " *"}</span>
        <textarea
          required={field.required}
          disabled={disabled}
          maxLength={field.maxLength}
          value={String(value ?? "")}
          onChange={(event) => set(event.target.value || null)}
          className="min-h-24 min-w-0 resize-y rounded-sm border border-border-subtle bg-surface-default px-3 py-2.5 outline-none focus:border-border-brand disabled:bg-surface-muted disabled:text-text-tertiary"
        />
      </label>
    );
  return (
    <label className="grid gap-1.5 text-sm font-medium">
      <span>
        {label}
        {field.required && " *"}
      </span>
      <input
        required={field.required}
        disabled={disabled}
        minLength={field.minLength}
        maxLength={field.maxLength}
        pattern={field.pattern}
        title={field.pattern ? (bs ? "Vrijednost nije u očekivanom formatu." : "The value is not in the expected format.") : undefined}
        type={field.type ?? "text"}
        step={field.type === "number" ? "any" : undefined}
        value={String(value ?? "").slice(0, field.type === "date" ? 10 : undefined)}
        onChange={(e) =>
          set(
            field.type === "number"
              ? e.target.value === ""
                ? null
                : Number(e.target.value)
              : e.target.value || null,
          )
        }
        onBlur={onBlur}
        aria-invalid={Boolean(error)}
        className="h-11 min-w-0 rounded-sm border border-border-subtle bg-surface-default px-3 outline-none focus:border-border-brand disabled:bg-surface-muted disabled:text-text-tertiary"
      />
      {error ? <span className="text-xs font-medium text-feedback-danger">{error}</span> : null}
    </label>
  );
}

function initialValue(field: ResourceField, record?: LegacyRecord) {
  const value = record?.[field.key] ?? record?.[field.key[0]!.toUpperCase() + field.key.slice(1)];
  return value ?? (field.type === "checkbox" ? false : "");
}
function normalizePayload(fields: readonly ResourceField[], values: Record<string, unknown>) {
  return Object.fromEntries(fields.map((field) => {
    const value = values[field.key];
    if (value === "" && (field.type === "number" || field.type === "date")) return [field.key, null];
    return [field.key, value === "" ? null : value];
  }));
}
function validateResource(resource: RegistryResource, values: Record<string, unknown>, bs: boolean) {
  const key = resource.key;
  const text = (field: string) => String(values[field] ?? "").trim();
  const missing = resource.capabilities?.fields.find((field) =>
    field.required
    && (!(["relatedToPersonId", "familyRelationshipType"].includes(field.key)) || isImmediateFamily(values["specialRelationBasis"]))
    && (values[field.key] === "" || values[field.key] == null));
  if (missing)
    return bs
      ? `Polje „${missing.labelBs}“ je obavezno.`
      : `The “${missing.labelEn}” field is required.`;
  if (key === "physicalPersons") {
    const residency = Number(values["residency"]);
    if (residency !== 1 && residency !== 2)
      return bs ? "Odaberite da li je lice rezident ili nerezident." : "Select whether the person is resident or non-resident.";
    if (residency === 1 && !/^\d{13}$/.test(text("jmbg")))
      return bs
        ? "Za rezidenta je obavezan JMBG od tačno 13 cifara."
        : "A resident must have a national ID containing exactly 13 digits.";
    if (residency === 2 && !text("passportNumber"))
      return bs
        ? "Za nerezidenta je obavezan broj pasoša."
        : "A passport number is required for a non-resident.";
    if (residency === 2 && !/^\d{1,10}$/.test(text("fbaId")))
      return bs
        ? "Za nerezidenta je obavezan FBA ID od najviše 10 cifara."
        : "A non-resident must have an FBA ID containing no more than 10 digits.";
    if (isImmediateFamily(values["specialRelationBasis"]) && !values["relatedToPersonId"])
      return bs ? "Odaberite fizičko lice s kojim je član uže porodice povezan." : "Select the individual to whom the immediate family member is related.";
    if (isImmediateFamily(values["specialRelationBasis"]) && !values["familyRelationshipType"])
      return bs ? "Odaberite porodični odnos." : "Select the family relationship.";
  }
  if (key === "legalPersons") {
    if (values["isResident"] === "" || values["isResident"] == null)
      return bs ? "Odaberite da li je pravno lice rezident ili nerezident." : "Select whether the legal entity is resident or non-resident.";
    const resident = Boolean(values["isResident"]);
    if (resident && !/^\d{13}$/.test(text("taxNumber")))
      return bs
        ? "Za rezidentno pravno lice obavezan je porezni broj od tačno 13 cifara."
        : "A resident legal entity must have a tax number containing exactly 13 digits.";
    if (resident && !isValidTaxNumber(text("taxNumber")))
      return bs
        ? "Kontrolna cifra poreznog broja nije ispravna. Provjerite uneseni broj."
        : "The tax-number check digit is invalid. Check the entered number.";
    if (!resident && !/^\d{1,10}$/.test(text("fbaId")))
      return bs
        ? "Za nerezidentno pravno lice obavezan je FBA ID."
        : "FBA ID is required for a non-resident legal entity.";
  }
  if (key === "limits") {
    for (const field of ["iznosLimita", "utilizacija", "korigovaniLimit"]) {
      const value = values[field];
      if (value != null && value !== "" && Number(value) < 0)
        return bs ? "Novčani iznosi ne mogu biti negativni." : "Monetary amounts cannot be negative.";
    }
  }
  const from = text("dateFrom");
  const to = text("dateTo");
  if (from && to && to < from)
    return bs ? "Datum do ne može biti prije datuma od." : "Date to cannot be before date from.";
  return "";
}

function isConditionallyDisabled(resourceKey: string, fieldKey: string, values: Record<string, unknown>) {
  if (resourceKey === "physicalPersons") {
    const residency = Number(values["residency"]);
    if (fieldKey === "jmbg") return residency === 2;
    if (fieldKey === "passportNumber" || fieldKey === "fbaId") return residency === 1;
    if (fieldKey === "relationDescription") return true;
    if (isImmediateFamily(values["specialRelationBasis"]) && [
      "isIdentifiedStaff", "connectedWithBank", "specialRelationshipWithBank",
      "specialContract", "malusClawback", "declarationNoFamilyMembers",
    ].includes(fieldKey)) return true;
  }
  if (resourceKey === "legalPersons") {
    if (fieldKey === "taxNumber") return values["isResident"] === false;
    if (fieldKey === "fbaId") return values["isResident"] === true;
    if (fieldKey === "connectionDescription") return true;
  }
  return false;
}

function updateFieldValues(resourceKey: string, field: ResourceField, value: unknown, current: Record<string, unknown>, bs: boolean) {
  const fieldKey = field.key;
  const selected = field.options?.find((option) => String(option.value) === String(value));
  const next = { ...current, [fieldKey]: value, ...(selected?.sets ?? {}), ...(bs ? selected?.setsBs : selected?.setsEn) };
  if (resourceKey === "physicalPersons" && fieldKey === "residency") {
    if (Number(value) === 1) {
      next["passportNumber"] = null;
      next["fbaId"] = null;
    } else if (Number(value) === 2) next["jmbg"] = null;
  }
  if (resourceKey === "physicalPersons" && fieldKey === "specialRelationBasis") {
    if (isImmediateFamily(value)) {
      next["isIdentifiedStaff"] = false;
      next["connectedWithBank"] = true;
      next["specialRelationshipWithBank"] = false;
      next["specialContract"] = false;
      next["malusClawback"] = false;
      next["declarationNoFamilyMembers"] = true;
    } else {
      next["relatedToPersonId"] = null;
      next["familyRelationshipType"] = null;
      next["isIdentifiedStaff"] = "";
      next["connectedWithBank"] = "";
      next["specialRelationshipWithBank"] = "";
      next["specialContract"] = "";
      next["malusClawback"] = "";
      next["declarationNoFamilyMembers"] = "";
    }
  }
  if (resourceKey === "legalPersons" && fieldKey === "isResident") {
    if (value === true) next["fbaId"] = null;
    if (value === false) next["taxNumber"] = null;
  }
  return next;
}
function isImmediateFamily(value: unknown) {
  const normalized = String(value ?? "").trim().toLocaleLowerCase();
  return normalized === "uza_porodica" || normalized === "član uže porodice povezanog lica";
}
function isValidTaxNumber(value: string) {
  if (!/^\d{13}$/.test(value)) return false;
  const weights = [7, 6, 5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
  const sum = weights.reduce((total, weight, index) => total + Number(value[index]) * weight, 0);
  const candidate = 11 - (sum % 11);
  const expected = candidate >= 10 ? 0 : candidate;
  return expected === Number(value[12]);
}
function fieldLabel(resource: RegistryResource, key: string, bs: boolean) {
  const field = resource.capabilities?.fields.find(
    (item) => item.key.toLowerCase() === key.toLowerCase(),
  );
  const known: Record<string, readonly [string, string]> = {
    statusLabel: ["Status", "Status"], residencyLabel: ["Rezidentnost", "Residency"],
    actionDisplay: ["Radnja", "Action"], tableName: ["Područje", "Area"],
    areaDisplay: ["Poslovno područje", "Business area"], changeSummary: ["Detalji promjene", "Change details"],
    username: ["Korisnik", "User"], timestamp: ["Vrijeme", "Time"],
    ipAddress: ["IP adresa", "IP address"], to: ["Primalac", "Recipient"],
    subject: ["Naslov", "Subject"], audience: ["Grupa primalaca", "Audience"], sentAt: ["Poslano", "Sent"],
    purpose: ["Poslovna svrha", "Business purpose"], deliveryStatus: ["Status dostave", "Delivery status"],
    personTypeLabel: ["Vrsta lica", "Person type"],
  };
  return field ? (bs ? field.labelBs : field.labelEn) : known[key]?.[bs ? 0 : 1] ?? humanize(key);
}
function isReadable(value: unknown) {
  return value == null || ["string", "number", "boolean"].includes(typeof value);
}
function humanize(value: string) {
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replaceAll("_", " ")
    .replace(/^./, (letter) => letter.toUpperCase());
}
function formatValue(value: unknown, bs: boolean, column?: string) {
  if (value == null || value === "") return "—";
  if (typeof value === "boolean") return <IconIndicator kind={value ? "yes" : "no"} label={value ? (bs ? "Da" : "Yes") : bs ? "Ne" : "No"} />;
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(value))
    return new Intl.DateTimeFormat(bs ? "bs-BA" : "en-GB", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
  if (typeof value === "string") {
    const labels: Record<string, readonly [IndicatorKind, string, string]> = {
      draft: ["draft", "Nacrt", "Draft"], verified: ["verified", "Verificirano", "Verified"], rejected: ["rejected", "Odbijeno", "Rejected"],
      resident: ["resident", "Rezident", "Resident"], nonresident: ["nonresident", "Nerezident", "Non-resident"],
      daily: ["daily", "Dnevni", "Daily"], monthly: ["monthly", "Mjesečni", "Monthly"],
    };
    const translated = labels[value.replaceAll("_", "").toLowerCase()];
    if (translated) return <IconIndicator kind={translated[0]} label={translated[bs ? 1 : 2]} />;
    if (column === "personTypeLabel") {
      const type = value.toLowerCase();
      const label = type.includes("zapos") || type === "employee" ? (bs ? "Zaposlenik" : "Employee") : type.includes("porod") || type === "familymember" ? (bs ? "Član porodice" : "Family member") : (bs ? "Povezano lice" : "Related person");
      return <IconIndicator kind={type.includes("zapos") || type === "employee" ? "employee" : type.includes("porod") || type === "familymember" ? "family" : "related"} label={label} />;
    }
  }
  return String(value);
}
function isIndicatorValue(value: unknown, column?: string) {
  if (typeof value === "boolean") return true;
  if (column === "personTypeLabel") return true;
  if (typeof value !== "string") return false;
  return ["draft", "verified", "rejected", "resident", "nonresident", "daily", "monthly"].includes(value.replaceAll("_", "").toLowerCase());
}
function validatePhysicalIdentity(values: Record<string, unknown>, bs: boolean) {
  const namePattern = /^[A-Za-zČĆŽŠĐčćžšđÀ-ž][A-Za-zČĆŽŠĐčćžšđÀ-ž '-]{1,99}$/;
  if (!namePattern.test(String(values["firstName"] ?? "").trim()) || !namePattern.test(String(values["lastName"] ?? "").trim())) return bs ? "Ime i prezime moraju imati najmanje dva slova i ne smiju sadržavati brojeve." : "First and last name must contain at least two letters and no numbers.";
  const residency = Number(values["residency"]);
  if (residency === 1 && !isValidJmbg(String(values["jmbg"] ?? ""))) return bs ? "JMBG nije ispravan: provjerite datum, 13 cifara i kontrolnu cifru." : "The national ID is invalid: check its date, 13 digits and check digit.";
  if (residency === 2 && !/^[A-Za-z0-9][A-Za-z0-9-]{4,19}$/.test(String(values["passportNumber"] ?? ""))) return bs ? "Broj pasoša mora sadržavati 5–20 slova, cifara ili crtica." : "Passport number must contain 5–20 letters, digits or hyphens.";
  if (!/^\d+$/.test(String(values["gccNumber"] ?? ""))) return bs ? "GCC broj smije sadržavati samo cifre." : "GCC number may contain digits only.";
  return "";
}
function isValidJmbg(value: string) {
  if (!/^\d{13}$/.test(value)) return false;
  const day = Number(value.slice(0, 2)); const month = Number(value.slice(2, 4)); const rawYear = Number(value.slice(4, 7));
  const year = rawYear > 900 ? 1000 + rawYear : 2000 + rawYear;
  const date = new Date(year, month - 1, day);
  if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day || date > new Date()) return false;
  const d = [...value].map(Number); const control = 11 - ((7 * (d[0]! + d[6]!) + 6 * (d[1]! + d[7]!) + 5 * (d[2]! + d[8]!) + 4 * (d[3]! + d[9]!) + 3 * (d[4]! + d[10]!) + 2 * (d[5]! + d[11]!)) % 11);
  return d[12] === (control > 9 ? 0 : control);
}
function isVerified(record: LegacyRecord) {
  const value = String(record["statusLabel"] ?? record["status"] ?? "").toLowerCase();
  return value === "verified" || value === "verificiran" || value === "3";
}
function errorMessage(error: unknown, bs: boolean) {
  return apiErrorMessage(
    error,
    bs
      ? "Podaci nisu sačuvani. Provjerite označena i obavezna polja pa pokušajte ponovo."
      : "The data was not saved. Check the selected and required fields, then try again.",
  );
}
function Loading({ label }: { label: string }) {
  return (
    <div className="space-y-3 p-6" aria-label={label}>
      {[1, 2, 3, 4].map((item) => (
        <div key={item} className="h-10 animate-pulse rounded-xs bg-surface-muted" />
      ))}
    </div>
  );
}
function ErrorState({ retry, bs }: { retry: () => void; bs: boolean }) {
  return (
    <div className="flex flex-col items-center px-6 py-16 text-center">
      <AlertCircle className="size-8 text-feedback-danger" />
      <h2 className="mt-4 font-bold">
        {bs ? "Podaci trenutno nisu dostupni" : "Data is currently unavailable"}
      </h2>
      <Text tone="secondary" className="mt-2 max-w-prose">
        {bs
          ? "Provjerite API, mrežu i prijavljenu sesiju."
          : "Check the API, network and authenticated session."}
      </Text>
      <Button className="mt-5" onClick={retry}>
        {bs ? "Pokušaj ponovo" : "Try again"}
      </Button>
    </div>
  );
}

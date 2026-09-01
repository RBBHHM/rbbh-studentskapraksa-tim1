import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Lock, Unlock } from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { Heading, Text } from "@/components/ui/typography";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { getLegacyRecords } from "@/lib/api/legacy-client";

type State = Readonly<Record<string, unknown>>;
export function PeriodPage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const query = useQuery({
    queryKey: ["period-current"],
    queryFn: () => apiClient.getLegacy<State>("/api/period-lock/current"),
  });
  const raw = (query.data?.["data"] ?? query.data) as State | undefined;
  const locked = Boolean(raw?.["isLocked"] ?? raw?.["IsLocked"]);
  const year = Number(raw?.["year"] ?? raw?.["Year"] ?? new Date().getFullYear());
  const month = Number(raw?.["month"] ?? raw?.["Month"] ?? new Date().getMonth() + 1);
  const [reason, setReason] = useState("");
  const [status, setStatus] = useState("");
  const [noteDialog, setNoteDialog] = useState<
    | { readonly kind: "process"; readonly id: string; readonly operation: "reject" | "request-info" }
    | { readonly kind: "respond"; readonly id: string }
    | null
  >(null);
  const [note, setNote] = useState("");
  const requests = useQuery({
    queryKey: ["unlock-requests", status],
    queryFn: () =>
      getLegacyRecords(`/api/period-lock/unlock-requests?status=${status}&page=1&pageSize=100`),
  });
  const action = useMutation({
    mutationFn: (lock: boolean) =>
      apiClient.postLegacy(`/api/period-lock/${lock ? "lock" : "unlock"}`, { body: {} }),
    onSuccess: async () => {
      toast.success(bs ? "Status perioda je promijenjen." : "Period status changed.");
      await cache.invalidateQueries({ queryKey: ["period-current"] });
      await cache.invalidateQueries({ queryKey: ["unlock-requests"] });
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Promjena nije uspjela." : "Change failed.")),
  });
  const requestUnlock = useMutation({
    mutationFn: () =>
      apiClient.postLegacy("/api/period-lock/request-unlock", {
        body: { reason, year, month },
      }),
    onSuccess: async () => {
      setReason("");
      toast.success(bs ? "Zahtjev je poslan." : "Request sent.");
      await cache.invalidateQueries({ queryKey: ["unlock-requests"] });
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Slanje zahtjeva nije uspjelo." : "Request failed.")),
  });
  const process = useMutation({
    mutationFn: ({ id, operation, value }: { id: string; operation: "reject" | "request-info"; value: string }) => {
      return apiClient.postLegacy(`/api/period-lock/unlock-requests/${id}/${operation}`, {
        body: { note: value },
      });
    },
    onSuccess: async () => {
      toast.success(bs ? "Zahtjev je obrađen." : "Request processed.");
      await cache.invalidateQueries({ queryKey: ["unlock-requests"] });
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Obrada zahtjeva nije uspjela." : "Request processing failed.")),
  });
  const approve = useMutation({
    mutationFn: ({ requestYear, requestMonth }: { requestYear: number; requestMonth: number }) =>
      apiClient.postLegacy("/api/period-lock/unlock", {
        body: { year: requestYear, month: requestMonth },
      }),
    onSuccess: async () => {
      toast.success(
        bs ? "Zahtjev je odobren i period otključan." : "Request approved and period unlocked.",
      );
      await cache.invalidateQueries({ queryKey: ["unlock-requests"] });
      await cache.invalidateQueries({ queryKey: ["period-current"] });
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Odobravanje nije uspjelo." : "Approval failed.")),
  });
  const respond = useMutation({
    mutationFn: ({ id, value }: { id: string; value: string }) => {
      return apiClient.postLegacy(`/api/period-lock/unlock-requests/${id}/respond`, {
        body: { message: value },
      });
    },
    onSuccess: async () => {
      toast.success(bs ? "Odgovor je poslan." : "Response sent.");
      await cache.invalidateQueries({ queryKey: ["unlock-requests"] });
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Slanje odgovora nije uspjelo." : "Sending the response failed.")),
  });
  return (
    <section>
      <Heading level={1} size={4}>
        {bs ? "Upravljanje periodom" : "Period management"}
      </Heading>
      <Text tone="secondary" className="mt-2">
        {bs
          ? "Zaključavanje perioda kontroliše dozvolu unosa i izmjene podataka."
          : "Period locking controls whether data can be created or changed."}
      </Text>
      <div className="mt-6 rounded-sm border border-border-subtle bg-surface-default p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-text-secondary">
              {bs ? "Trenutni status" : "Current status"}
            </p>
            <p className="mt-1 text-xl font-bold">
              {query.isLoading
                ? "…"
                : locked
                  ? bs
                    ? "Period je zaključan"
                    : "Period is locked"
                  : bs
                    ? "Period je otvoren"
                    : "Period is open"}
            </p>
            <p className="mt-1 text-sm text-text-secondary">
              {String(raw?.["year"] ?? raw?.["Year"] ?? "")} /{" "}
              {String(raw?.["month"] ?? raw?.["Month"] ?? "")}
            </p>
          </div>
          <Button
            disabled={query.isLoading || action.isPending}
            variant={locked ? "secondary" : "primary"}
            onClick={() => action.mutate(!locked)}
          >
            {locked ? <Unlock className="size-4" /> : <Lock className="size-4" />}
            {locked
              ? bs
                ? "Otključaj period"
                : "Unlock period"
              : bs
                ? "Zaključaj period"
                : "Lock period"}
          </Button>
        </div>
      </div>
      {locked && (
        <form
          className="mt-5 rounded-sm border border-border-subtle bg-surface-default p-5"
          onSubmit={(event) => {
            event.preventDefault();
            requestUnlock.mutate();
          }}
        >
          <label className="grid gap-1 text-sm font-medium">
            {bs ? "Razlog zahtjeva za otključavanje" : "Unlock request reason"}
            <textarea
              required
              minLength={10}
              maxLength={500}
              className="min-h-24 rounded-sm border border-border-subtle bg-surface-default p-3"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
            />
          </label>
          <Button className="mt-3" type="submit" disabled={requestUnlock.isPending}>
            {bs ? "Pošalji zahtjev" : "Send request"}
          </Button>
        </form>
      )}
      <div className="mt-8 flex flex-wrap items-end justify-between gap-3">
        <Heading level={2} size={3}>
          {bs ? "Zahtjevi za otključavanje" : "Unlock requests"}
        </Heading>
        <label className="grid gap-1 text-sm font-medium">
          {bs ? "Status" : "Status"}
          <select
            className="h-10 rounded-sm border border-border-subtle bg-surface-default px-3"
            value={status}
            onChange={(event) => setStatus(event.target.value)}
          >
            <option value="">{bs ? "Svi" : "All"}</option>
            <option value="PENDING">{bs ? "Čeka obradu" : "Pending"}</option>
            <option value="NEEDS_INFO">{bs ? "Potrebna dopuna" : "More information needed"}</option>
            <option value="APPROVED">{bs ? "Odobren" : "Approved"}</option>
            <option value="REJECTED">{bs ? "Odbijen" : "Rejected"}</option>
          </select>
        </label>
      </div>
      <div className="mt-4 overflow-x-auto rounded-sm border border-border-subtle bg-surface-default">
        <table className="w-full min-w-[800px] text-left text-sm">
          <thead className="bg-surface-subtle">
            <tr>
              {[
                bs ? "Podnosilac" : "Requested by",
                bs ? "Period" : "Period",
                bs ? "Razlog" : "Reason",
                "Status",
                bs ? "Napomena" : "Note",
                bs ? "Akcije" : "Actions",
              ].map((label, index) => (
                <th key={label} className={`px-4 py-3 ${index === 5 ? "text-center" : ""}`}>
                  {label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {(requests.data ?? []).map((item, index) => {
              const id = String(item["id"] ?? index);
              const itemStatus = String(item["status"] ?? "");
              return (
                <tr key={id}>
                  <td className="px-4 py-3">{String(item["requestedBy"] ?? "—")}</td>
                  <td className="px-4 py-3">
                    {String(item["month"] ?? "")}/{String(item["year"] ?? "")}
                  </td>
                  <td className="max-w-80 break-words px-4 py-3">
                    {String(item["reason"] ?? "—")}
                  </td>
                  <td className="px-4 py-3">{requestStatusLabel(itemStatus, bs)}</td>
                  <td className="max-w-72 break-words px-4 py-3">
                    {String(item["adminNote"] ?? "—")}
                  </td>
                  <td className="px-4 py-2 text-center align-middle">
                    <div className="flex flex-wrap items-center justify-center gap-1">
                      {itemStatus === "PENDING" && (
                        <>
                          <Button
                            size="sm"
                            onClick={() =>
                              approve.mutate({
                                requestYear: Number(item["year"]),
                                requestMonth: Number(item["month"]),
                              })
                            }
                          >
                            {bs ? "Odobri/otključaj" : "Approve/unlock"}
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => { setNote(""); setNoteDialog({ kind: "process", id, operation: "reject" }); }}
                          >
                            {bs ? "Odbij" : "Reject"}
                          </Button>
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => { setNote(""); setNoteDialog({ kind: "process", id, operation: "request-info" }); }}
                          >
                            {bs ? "Traži dopunu" : "Request info"}
                          </Button>
                        </>
                      )}
                      {itemStatus === "NEEDS_INFO" && (
                        <Button size="sm" onClick={() => { setNote(""); setNoteDialog({ kind: "respond", id }); }}>
                          {bs ? "Dostavi traženu dopunu" : "Provide requested information"}
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <Dialog open={Boolean(noteDialog)} onOpenChange={(open) => !open && setNoteDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {noteDialog?.kind === "respond"
                ? bs ? "Dopuna zahtjeva" : "Request clarification"
                : bs ? "Napomena administratora" : "Administrator note"}
            </DialogTitle>
            <DialogDescription>
              {noteDialog?.kind === "respond"
                ? bs ? "Administrator je zatražio dodatne informacije. Napišite dopunu od najmanje 10 znakova; nakon slanja zahtjev se vraća na obradu." : "The administrator requested more information. Enter at least 10 characters; the request will return to review after sending."
                : bs ? "Unesite jasno obrazloženje od najmanje 10 znakova prije nastavka." : "Enter a clear explanation of at least 10 characters before continuing."}
            </DialogDescription>
          </DialogHeader>
          <Textarea
            autoFocus
            required
            minLength={10}
            maxLength={500}
            value={note}
            onChange={(event) => setNote(event.target.value)}
            placeholder={bs ? "Unesite napomenu…" : "Enter a note…"}
          />
          <p className={`text-xs ${note.trim().length > 0 && note.trim().length < 10 ? "font-semibold text-feedback-danger" : "text-text-secondary"}`}>{bs ? `Najmanje 10 znakova — uneseno ${note.trim().length}.` : `At least 10 characters — ${note.trim().length} entered.`}</p>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setNoteDialog(null)}>
              {bs ? "Odustani" : "Cancel"}
            </Button>
            <Button
              disabled={note.trim().length < 10 || process.isPending || respond.isPending}
              onClick={() => {
                if (!noteDialog || note.trim().length < 10) return;
                const value = note.trim();
                if (noteDialog.kind === "respond") respond.mutate({ id: noteDialog.id, value });
                else process.mutate({ id: noteDialog.id, operation: noteDialog.operation, value });
                setNoteDialog(null);
              }}
            >
              {bs ? "Potvrdi" : "Confirm"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </section>
  );
}

function requestStatusLabel(status: string, bs: boolean) {
  const labels: Record<string, readonly [string, string]> = {
    PENDING: ["Čeka obradu", "Pending"], NEEDS_INFO: ["Potrebna dopuna", "More information needed"],
    APPROVED: ["Odobren", "Approved"], REJECTED: ["Odbijen", "Rejected"],
  };
  return labels[status]?.[bs ? 0 : 1] ?? status;
}

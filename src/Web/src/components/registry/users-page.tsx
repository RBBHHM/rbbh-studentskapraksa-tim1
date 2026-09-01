import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Power, Trash2, UserCog, X } from "lucide-react";
import { type FormEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { IconIndicator } from "@/components/registry/icon-indicator";
import { Heading, Text } from "@/components/ui/typography";
import { apiClient, apiErrorMessage } from "@/lib/api/http-client";
import { getLegacyRecords, type LegacyRecord } from "@/lib/api/legacy-client";

export function UsersPage() {
  const { i18n } = useTranslation();
  const bs = i18n.language.startsWith("bs");
  const cache = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [roleUser, setRoleUser] = useState<LegacyRecord>();
  const [stateUser, setStateUser] = useState<{ id: string; active: boolean; label: string }>();
  const [deleteUser, setDeleteUser] = useState<{ id: string; label: string }>();
  const users = useQuery({ queryKey: ["users"], queryFn: () => getLegacyRecords("/api/users") });
  const roles = useQuery({ queryKey: ["roles"], queryFn: () => getLegacyRecords("/api/roles") });
  const refresh = async () => cache.invalidateQueries({ queryKey: ["users"] });
  const state = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active
        ? apiClient.deleteLegacy(`/api/users/${id}`)
        : apiClient.postLegacy(`/api/users/${id}/reactivate`),
    onSuccess: async () => {
      toast.success(bs ? "Status korisnika je promijenjen." : "User status changed.");
      await refresh();
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Promjena statusa nije uspjela." : "Status change failed.")),
  });
  const remove = useMutation({
    mutationFn: (id: string) => apiClient.deleteLegacy(`/api/users/${id}/permanent`),
    onSuccess: async () => { toast.success(bs ? "Korisnik je obrisan." : "User deleted."); await refresh(); },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Brisanje korisnika nije uspjelo." : "Deleting the user failed.")),
  });
  return (
    <section>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <Heading level={1} size={4}>
            {bs ? "Upravljanje korisnicima" : "User management"}
          </Heading>
          <Text tone="secondary" className="mt-2">
            {bs
              ? "Dodijelite jedan ili više nezavisnih pristupa poslovnim područjima."
              : "Assign one or more independent business-area accesses."}
          </Text>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="size-4" />
          {bs ? "Dodaj korisnika" : "Add user"}
        </Button>
      </div>
      <div className="mt-6 overflow-x-auto rounded-sm border border-border-subtle bg-surface-default">
        <table className="w-full min-w-[850px] text-left text-sm">
          <thead className="bg-surface-subtle">
            <tr>
              {[
                bs ? "Korisnik" : "User",
                "Email",
                bs ? "Pristupi" : "Accesses",
                bs ? "Status" : "Status",
                bs ? "Akcije" : "Actions",
              ].map((x) => (
                <th key={x} className="px-4 py-3">
                  {x}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {(users.data ?? []).map((u, i) => {
              const id = String(u["id"] ?? i);
              const active = readBoolean(u["isActive"] ?? u["aktivan"] ?? u["Aktivan"]);
              return (
                <tr key={id}>
                  <td className="px-4 py-3 font-semibold">
                    {String(u["firstName"] ?? "")} {String(u["lastName"] ?? "")}
                    <span className="block font-normal text-text-secondary">
                      {String(u["username"] ?? "")}
                    </span>
                  </td>
                  <td className="px-4 py-3">{String(u["email"] ?? "—")}</td>
                  <td className="px-4 py-3">
                    {Array.isArray(u["roles"]) ? u["roles"].map((role) => accessLabel(String(role), bs)).join(", ") : "—"}
                  </td>
                  <td className="px-4 py-3 text-center align-middle">
                    <div className="flex items-center justify-center">
                      <IconIndicator kind={active ? "active" : "inactive"} label={active ? (bs ? "Aktivan" : "Active") : bs ? "Neaktivan" : "Inactive"} />
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex items-center justify-center gap-1">
                      <Button
                        size="icon"
                        variant="ghost"
                        title={bs ? "Promijeni ulogu" : "Change role"}
                        onClick={() => setRoleUser(u)}
                      >
                        <UserCog className="size-4" />
                      </Button>
                      <Button
                        size="icon"
                        variant="ghost"
                        title={
                          active
                            ? bs
                              ? "Deaktiviraj"
                              : "Deactivate"
                            : bs
                              ? "Reaktiviraj"
                              : "Reactivate"
                        }
                        onClick={() => setStateUser({ id, active, label: String(u["username"] ?? "") })}
                      >
                        <Power className="size-4" />
                      </Button>
                      <Button size="icon" variant="ghost" title={bs ? "Obriši korisnika" : "Delete user"} onClick={() => setDeleteUser({ id, label: String(u["username"] ?? "") })}>
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
      {createOpen && (
        <UserForm
          bs={bs}
          roles={roles.data ?? []}
          close={() => setCreateOpen(false)}
          saved={refresh}
        />
      )}
      {roleUser && (
        <RoleForm
          bs={bs}
          user={roleUser}
          roles={roles.data ?? []}
          close={() => setRoleUser(undefined)}
          saved={refresh}
        />
      )}
      <ConfirmDialog
        open={Boolean(stateUser)}
        title={stateUser?.active
          ? bs ? "Deaktivirati korisnika?" : "Deactivate user?"
          : bs ? "Reaktivirati korisnika?" : "Reactivate user?"}
        description={bs
          ? `Status korisnika ${stateUser?.label ?? ""} će biti promijenjen.`
          : `The status of ${stateUser?.label ?? "this user"} will be changed.`}
        cancelLabel={bs ? "Odustani" : "Cancel"}
        confirmLabel={stateUser?.active ? (bs ? "Deaktiviraj" : "Deactivate") : (bs ? "Reaktiviraj" : "Reactivate")}
        destructive={Boolean(stateUser?.active)}
        onCancel={() => setStateUser(undefined)}
        onConfirm={() => { if (stateUser) state.mutate(stateUser); setStateUser(undefined); }}
      />
      <ConfirmDialog open={Boolean(deleteUser)} title={bs ? "Obrisati korisnika?" : "Delete user?"} description={bs ? `Korisnik ${deleteUser?.label ?? ""} i njegovi pristupi bit će trajno uklonjeni.` : `${deleteUser?.label ?? "This user"} and their accesses will be permanently removed.`} cancelLabel={bs ? "Odustani" : "Cancel"} confirmLabel={bs ? "Obriši" : "Delete"} destructive onCancel={() => setDeleteUser(undefined)} onConfirm={() => { if (deleteUser) remove.mutate(deleteUser.id); setDeleteUser(undefined); }} />
    </section>
  );
}

function readBoolean(value: unknown): boolean {
  if (typeof value === "boolean") return value;
  if (typeof value === "number") return value === 1;
  if (typeof value === "string") return !["false", "0", "no", "ne", "inactive", "neaktivan"].includes(value.trim().toLowerCase());
  return Boolean(value);
}

function UserForm({
  bs,
  roles,
  close,
  saved,
}: {
  bs: boolean;
  roles: readonly LegacyRecord[];
  close: () => void;
  saved: () => Promise<unknown>;
}) {
  const requiredEmailDomain = "@raiffeisengroup.ba";
  const [v, setV] = useState({
    username: "",
    firstName: "",
    lastName: "",
    email: "",
    roleIds: [] as string[],
    isActive: true,
  });
  const [validationError, setValidationError] = useState("");
  const m = useMutation({
    mutationFn: () => apiClient.postLegacy("/api/users", { body: v }),
    onSuccess: async () => {
      toast.success(bs ? "Korisnik je kreiran." : "User created.");
      await saved();
      close();
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Kreiranje nije uspjelo." : "Creation failed.")),
  });
  return (
    <Modal title={bs ? "Novi korisnik" : "New user"} close={close}>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (v.roleIds.length === 0) { setValidationError(bs ? "Odaberite najmanje jedan funkcionalni pristup." : "Select at least one functional access."); return; }
          if (!v.email.trim().toLocaleLowerCase().endsWith(requiredEmailDomain)) {
            setValidationError(
              bs
                ? `Email adresa mora završavati domenom ${requiredEmailDomain}.`
                : `The email address must end with ${requiredEmailDomain}.`,
            );
            return;
          }
          setValidationError("");
          m.mutate();
        }}
        className="grid gap-4 sm:grid-cols-2"
      >
        {(
          [
            ["username", bs ? "Korisničko ime" : "Username"],
            ["firstName", bs ? "Ime" : "First name"],
            ["lastName", bs ? "Prezime" : "Last name"],
            ["email", "Email"],
          ] as const
        ).map(([k, l]) => (
          <label key={k} className="grid gap-1 text-sm font-medium">
            {l}
            <input
              required
              type={k === "email" ? "email" : "text"}
              placeholder={k === "email" ? `ime.prezime${requiredEmailDomain}` : undefined}
              className="h-11 rounded-sm border border-border-subtle bg-surface-default px-3"
              value={v[k]}
              onChange={(e) => {
                setV({ ...v, [k]: e.target.value });
                if (validationError) setValidationError("");
              }}
            />
          </label>
        ))}
        <div className="sm:col-span-2">
          <AccessSelector bs={bs} roles={roles} selected={v.roleIds} setSelected={(roleIds) => setV({ ...v, roleIds })} />
        </div>
        {validationError && <p role="alert" className="sm:col-span-2 rounded-sm border border-feedback-danger bg-feedback-danger/10 p-3 text-sm font-semibold text-feedback-danger">{validationError}</p>}
        <Actions bs={bs} close={close} pending={m.isPending} />
      </form>
    </Modal>
  );
}
function RoleForm({
  bs,
  user,
  roles,
  close,
  saved,
}: {
  bs: boolean;
  user: LegacyRecord;
  roles: readonly LegacyRecord[];
  close: () => void;
  saved: () => Promise<unknown>;
}) {
  const currentNames = Array.isArray(user["roles"]) ? user["roles"].map(String) : [];
  const [roleIds, setRoleIds] = useState(() => roles
    .filter((role) => currentNames.includes(String(role["name"])))
    .map((role) => String(role["id"])));
  const id = String(user["id"]);
  const m = useMutation({
    mutationFn: () => apiClient.putLegacy(`/api/users/${id}/role`, { body: { roleIds } }),
    onSuccess: async () => {
      toast.success(bs ? "Uloga je promijenjena." : "Role changed.");
      await saved();
      close();
    },
    onError: (error) => toast.error(apiErrorMessage(error, bs ? "Promjena uloge nije uspjela." : "Role change failed.")),
  });
  return (
    <Modal title={bs ? "Promjena funkcionalnih pristupa" : "Change functional accesses"} close={close}>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (roleIds.length === 0) return;
          m.mutate();
        }}
      >
        <AccessSelector bs={bs} roles={roles} selected={roleIds} setSelected={setRoleIds} />
        <Actions bs={bs} close={close} pending={m.isPending} />
      </form>
    </Modal>
  );
}

function AccessSelector({ bs, roles, selected, setSelected }: { bs: boolean; roles: readonly LegacyRecord[]; selected: string[]; setSelected: (ids: string[]) => void }) {
  return (
    <fieldset>
      <legend className="text-sm font-semibold">{bs ? "Funkcionalni pristupi *" : "Functional accesses *"}</legend>
      <p className="mt-1 text-sm text-text-secondary">{bs ? "Korisnik može imati više pristupa istovremeno." : "A user can have several accesses at the same time."}</p>
      <div className="mt-3 grid gap-2 sm:grid-cols-2">
        {roles.map((role) => {
          const id = String(role["id"]);
          const name = String(role["name"]);
          return (
            <label key={id} className="flex min-h-12 items-center gap-3 rounded-sm border border-border-subtle bg-surface-default px-3 text-sm font-medium">
              <input type="checkbox" className="size-4" checked={selected.includes(id)} onChange={(event) => setSelected(event.target.checked ? [...selected, id] : selected.filter((item) => item !== id))} />
              {accessLabel(name, bs)}
            </label>
          );
        })}
      </div>
      {selected.length === 0 && <p className="mt-2 text-sm text-feedback-danger">{bs ? "Odaberite najmanje jedan pristup." : "Select at least one access."}</p>}
    </fieldset>
  );
}

function accessLabel(name: string, bs: boolean) {
  const labels: Record<string, [string, string]> = {
    "physical-persons": ["Fizička lica", "Individuals"],
    "legal-persons": ["Pravna lica", "Legal entities"],
    limits: ["Limiti", "Limits"],
    "regulatory-reporting": ["Regulatorna izvještavanja", "Regulatory reporting"],
  };
  return labels[name]?.[bs ? 0 : 1] ?? name;
}
function Modal({
  title,
  close,
  children,
}: {
  title: string;
  close: () => void;
  children: React.ReactNode;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}>
      <div className="w-full max-w-3xl rounded-sm border border-border-subtle bg-surface-raised p-6 text-text-primary shadow-2xl">
        <div className="mb-5 flex justify-between">
          <Heading level={2} size={3}>
            {title}
          </Heading>
          <Button size="icon" variant="ghost" onClick={close}>
            <X className="size-5" />
          </Button>
        </div>
        {children}
      </div>
    </div>
  );
}
function Actions({ bs, close, pending }: { bs: boolean; close: () => void; pending: boolean }) {
  return (
    <div className="mt-3 flex justify-end gap-2 sm:col-span-2">
      <Button type="button" variant="secondary" onClick={close}>
        {bs ? "Odustani" : "Cancel"}
      </Button>
      <Button type="submit" disabled={pending}>
        {bs ? "Sačuvaj" : "Save"}
      </Button>
    </div>
  );
}

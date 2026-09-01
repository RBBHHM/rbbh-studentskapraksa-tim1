import { useEffect, useState } from "react";
import { useSafeTranslation } from "@/localization";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Text } from "@/components/ui/typography";
import {
  accountCurrencies,
  accountSegmentOptions,
  accountStatusMeta,
  accountStatuses,
} from "@/lib/admin/account-presentation";
import {
  emptyAccountDraft,
  validateDraft,
  type AccountDraft,
  type DraftErrors,
} from "@/lib/admin/use-account-directory";
import type { Account, AccountSegment, AccountStatus } from "@/lib/api/contract";

/**
 * Create/edit dialog for an account.
 *
 * One dialog serves both operations: the caller passes the record being edited,
 * or nothing to create. Option lists come from the presentation module, so the
 * form can never drift from the contract's allowed values.
 */
export interface AccountFormDialogProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  /** Omit to create a new record. */
  readonly account?: Account | undefined;
  readonly onSubmit: (draft: AccountDraft) => void;
}

/** Balances are edited in major units and stored in minor units. */
const toMajor = (minor: number) => (minor / 100).toString();
const toMinor = (major: string) => Math.round(Number.parseFloat(major || "0") * 100);

export function AccountFormDialog({ open, onOpenChange, account, onSubmit }: AccountFormDialogProps) {
  const t = useSafeTranslation("admin");
  const [draft, setDraft] = useState<AccountDraft>(emptyAccountDraft);
  const [balanceInput, setBalanceInput] = useState("0");
  const [errors, setErrors] = useState<DraftErrors>({});

  /* Re-seed the fields whenever the dialog opens for a different record. */
  useEffect(() => {
    if (!open) return;
    const next: AccountDraft = account
      ? {
          name: account.name,
          iban: account.iban,
          currency: account.currency,
          segment: account.segment,
          status: account.status,
          balanceMinor: account.balanceMinor,
        }
      : emptyAccountDraft;
    setDraft(next);
    setBalanceInput(toMajor(next.balanceMinor));
    setErrors({});
  }, [open, account]);

  function patch(values: Partial<AccountDraft>) {
    setDraft((current) => ({ ...current, ...values }));
  }

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const candidate: AccountDraft = { ...draft, balanceMinor: toMinor(balanceInput) };
    const found = validateDraft(candidate);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    onSubmit(candidate);
    onOpenChange(false);
  }

  const isEdit = account !== undefined;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? t.text("form.title.edit", { name: account.name }) : t.text("form.title.create")}
          </DialogTitle>
          <DialogDescription>
            {isEdit ? t.text("form.description.edit") : t.text("form.description.create")}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} noValidate className="grid gap-4">
          <Field id="account-name" label={t.text("form.fields.name.label")} error={errors.name ? t.text("form.fields.name.error") : undefined}>
            <Input
              id="account-name"
              value={draft.name}
              onChange={(event) => patch({ name: event.target.value })}
              aria-invalid={errors.name !== undefined}
              placeholder={t.text("form.fields.name.placeholder")}
              autoComplete="off"
            />
          </Field>

          <Field id="account-iban" label={t.text("form.fields.iban.label")} error={errors.iban ? t.text("form.fields.iban.error") : undefined}>
            <Input
              id="account-iban"
              value={draft.iban}
              onChange={(event) => patch({ iban: event.target.value })}
              aria-invalid={errors.iban !== undefined}
              placeholder={t.text("form.fields.iban.placeholder")}
              className="font-mono"
              autoComplete="off"
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="account-segment" label={t.text("form.fields.segment.label")}>
              <Select
                value={draft.segment}
                onValueChange={(value) => patch({ segment: value as AccountSegment })}
              >
                <SelectTrigger id="account-segment">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {accountSegmentOptions.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            <Field id="account-status" label={t.text("form.fields.status.label")}>
              <Select
                value={draft.status}
                onValueChange={(value) => patch({ status: value as AccountStatus })}
              >
                <SelectTrigger id="account-status">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {accountStatuses.map((option) => (
                    <SelectItem key={option} value={option}>
                      {t.text(`status.${option}.label`)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            <Field id="account-currency" label={t.text("form.fields.currency.label")}>
              <Select
                value={draft.currency}
                onValueChange={(value) => patch({ currency: value as Account["currency"] })}
              >
                <SelectTrigger id="account-currency">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {accountCurrencies.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            <Field id="account-balance" label={t.text("form.fields.balance.label")} error={errors.balanceMinor ? t.text("form.fields.balance.error") : undefined}>
              <Input
                id="account-balance"
                inputMode="decimal"
                value={balanceInput}
                onChange={(event) => setBalanceInput(event.target.value)}
                aria-invalid={errors.balanceMinor !== undefined}
                className="tabular-nums"
              />
            </Field>
          </div>

          <DialogFooter className="mt-2 gap-2">
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              {t.text("form.cancel")}
            </Button>
            <Button type="submit">
              {isEdit ? t.text("form.submit.edit") : t.text("form.submit.create")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

/** Label + control + error message, so every field is wired identically. */
function Field({
  id,
  label,
  error,
  children,
}: {
  id: string;
  label: string;
  error?: string | undefined;
  children: React.ReactNode;
}) {
  return (
    <div>
      <Label htmlFor={id}>{label}</Label>
      <div className="mt-1.5">{children}</div>
      {error ? (
        <Text size="xs" tone="danger" role="alert" className="mt-1.5">
          {error}
        </Text>
      ) : null}
    </div>
  );
}

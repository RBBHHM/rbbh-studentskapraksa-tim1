import { useState } from "react";
import { useTranslation } from "react-i18next";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Check } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Text } from "@/components/ui/typography";
import { cn } from "@/lib/utils";
import {
  wizardCurrencyOptions,
  wizardDefaultValues,
  wizardSchema,
  wizardSegmentOptions,
  wizardStepIds,
  type WizardStepId,
  type WizardValues,
} from "@/lib/patterns/wizard";

/** Multi-step onboarding form: local state, react-hook-form + zod, review before submit. */
export function WizardPattern() {
  const { t } = useTranslation("patterns");
  const [stepIndex, setStepIndex] = useState(0);
  const stepId: WizardStepId = wizardStepIds[stepIndex] ?? wizardStepIds[0];

  const form = useForm<WizardValues>({
    resolver: zodResolver(wizardSchema),
    defaultValues: wizardDefaultValues,
    mode: "onBlur",
  });

  const fieldsByStep: Record<WizardStepId, readonly (keyof WizardValues)[]> = {
    company: ["companyName", "segment"],
    contact: ["contactName", "contactEmail"],
    billing: ["iban", "currency"],
    review: [],
  };

  async function goNext() {
    const valid = await form.trigger(fieldsByStep[stepId]);
    if (valid) setStepIndex((i) => Math.min(i + 1, wizardStepIds.length - 1));
  }

  const values = form.watch();

  return (
    <div className="mx-auto w-full max-w-xl">
      <ol className="flex items-center gap-2">
        {wizardStepIds.map((id, index) => (
          <li key={id} className="flex flex-1 items-center gap-2">
            <span
              className={cn(
                "flex size-7 shrink-0 items-center justify-center rounded-full border text-xs font-bold",
                index < stepIndex
                  ? "border-border-brand bg-surface-brand text-text-on-brand"
                  : index === stepIndex
                    ? "border-border-strong text-text-primary"
                    : "border-border-subtle text-text-tertiary",
              )}
            >
              {index < stepIndex ? <Check aria-hidden="true" className="size-4" /> : index + 1}
            </span>
            <span className={cn("hidden text-xs sm:inline", index === stepIndex ? "font-bold text-text-primary" : "text-text-tertiary")}>
              {t(`wizardPattern.steps.${id}.label` as never) as string}
            </span>
            {index < wizardStepIds.length - 1 ? <span aria-hidden="true" className="h-px flex-1 bg-border-subtle" /> : null}
          </li>
        ))}
      </ol>

      <div className="mt-8 rounded-sm border border-border-default p-6">
        {stepId === "company" ? (
          <div className="space-y-4">
            <div>
              <Label htmlFor="wiz-company">{t("wizardPattern.fields.companyName")}</Label>
              <Input id="wiz-company" className="mt-1.5" {...form.register("companyName")} />
              {form.formState.errors.companyName ? (
                <Text size="sm" tone="danger" className="mt-1">
                  {t(form.formState.errors.companyName.message as never) as string}
                </Text>
              ) : null}
            </div>
            <div>
              <Label htmlFor="wiz-segment">{t("wizardPattern.fields.segment")}</Label>
              <Select value={values.segment} onValueChange={(v) => form.setValue("segment", v, { shouldValidate: true })}>
                <SelectTrigger id="wiz-segment" className="mt-1.5">
                  <SelectValue placeholder={t("wizardPattern.fields.segmentPlaceholder")} />
                </SelectTrigger>
                <SelectContent>
                  {wizardSegmentOptions.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        ) : null}

        {stepId === "contact" ? (
          <div className="space-y-4">
            <div>
              <Label htmlFor="wiz-contact-name">{t("wizardPattern.fields.contactName")}</Label>
              <Input id="wiz-contact-name" className="mt-1.5" {...form.register("contactName")} />
            </div>
            <div>
              <Label htmlFor="wiz-contact-email">{t("wizardPattern.fields.contactEmail")}</Label>
              <Input id="wiz-contact-email" type="email" className="mt-1.5" {...form.register("contactEmail")} />
              {form.formState.errors.contactEmail ? (
                <Text size="sm" tone="danger" className="mt-1">
                  {t(form.formState.errors.contactEmail.message as never) as string}
                </Text>
              ) : null}
            </div>
          </div>
        ) : null}

        {stepId === "billing" ? (
          <div className="space-y-4">
            <div>
              <Label htmlFor="wiz-iban">{t("wizardPattern.fields.iban")}</Label>
              <Input id="wiz-iban" className="mt-1.5 font-mono" {...form.register("iban")} />
            </div>
            <div>
              <Label htmlFor="wiz-currency">{t("wizardPattern.fields.currency")}</Label>
              <Select value={values.currency} onValueChange={(v) => form.setValue("currency", v, { shouldValidate: true })}>
                <SelectTrigger id="wiz-currency" className="mt-1.5">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {wizardCurrencyOptions.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        ) : null}

        {stepId === "review" ? (
          <dl className="grid gap-3 sm:grid-cols-2">
            {(
              [
                ["companyName", values.companyName],
                ["segment", values.segment],
                ["contactName", values.contactName],
                ["contactEmail", values.contactEmail],
                ["iban", values.iban],
                ["currency", values.currency],
              ] as const
            ).map(([key, value]) => (
              <div key={key}>
                <dt className="text-xs text-text-tertiary">{t(`wizardPattern.fields.${key}` as never) as string}</dt>
                <dd className="text-sm font-medium text-text-primary">{value || "—"}</dd>
              </div>
            ))}
          </dl>
        ) : null}
      </div>

      <div className="mt-4 flex justify-between">
        <Button variant="ghost" disabled={stepIndex === 0} onClick={() => setStepIndex((i) => Math.max(0, i - 1))}>
          {t("wizardPattern.back")}
        </Button>
        {stepId === "review" ? (
          <Button onClick={() => setStepIndex(0)}>{t("wizardPattern.submit")}</Button>
        ) : (
          <Button onClick={goNext}>{t("wizardPattern.next")}</Button>
        )}
      </div>
    </div>
  );
}

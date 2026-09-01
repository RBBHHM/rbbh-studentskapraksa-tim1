/**
 * Wizard pattern: step definitions and the schema each step validates.
 *
 * Local state only — nothing is submitted anywhere — but the shape mirrors a
 * real onboarding form so the pattern demonstrates the same
 * react-hook-form + zod wiring the rest of the system uses.
 */
import { z } from "zod";

export const wizardStepIds = ["company", "contact", "billing", "review"] as const;
export type WizardStepId = (typeof wizardStepIds)[number];

export const companyStepSchema = z.object({
  companyName: z.string().min(2, "wizardPattern.errors.required"),
  segment: z.string().min(1, "wizardPattern.errors.required"),
});

export const contactStepSchema = z.object({
  contactName: z.string().min(2, "wizardPattern.errors.required"),
  contactEmail: z.string().email("wizardPattern.errors.email"),
});

export const billingStepSchema = z.object({
  iban: z.string().min(8, "wizardPattern.errors.required"),
  currency: z.string().min(1, "wizardPattern.errors.required"),
});

export const wizardSchema = companyStepSchema.merge(contactStepSchema).merge(billingStepSchema);
export type WizardValues = z.infer<typeof wizardSchema>;

export const wizardDefaultValues: WizardValues = {
  companyName: "",
  segment: "",
  contactName: "",
  contactEmail: "",
  iban: "",
  currency: "EUR",
};

export const wizardSegmentOptions = ["Corporate", "Institutional", "Treasury"] as const;
export const wizardCurrencyOptions = ["EUR", "RON", "CZK"] as const;

import "i18next";

import type { LocalizationResources } from "./types/resource-keys";

/**
 * Augments i18next so `useTranslation("navigation").t("primary.overview")` is
 * checked by the compiler. Keys come from the source locale bundles; a typo is a
 * build error rather than a blank label at runtime.
 */
declare module "i18next" {
  interface CustomTypeOptions {
    defaultNS: "common";
    resources: LocalizationResources;
    returnNull: false;
  }
}

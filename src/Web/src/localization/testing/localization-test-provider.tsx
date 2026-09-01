import * as React from "react";
import { I18nextProvider } from "react-i18next";
import i18next, { type i18n as I18nInstance, type Resource } from "i18next";
import ICU from "i18next-icu";

import { pseudoLocalePostProcessor, pseudoLocalePostProcessorName } from "./pseudo-localization";
import { localizationNamespaces } from "../config/localization-config";

/**
 * Test/Storybook localization provider.
 *
 * Uses in-memory resources instead of the runtime delivery pipeline, so tests
 * and stories are deterministic and never touch Phrase or Azure. This is
 * development tooling and shares no code path with production delivery besides
 * the same i18next configuration shape.
 */

export interface LocalizationTestProviderProps {
  readonly children: React.ReactNode;
  readonly locale?: string;
  readonly direction?: "ltr" | "rtl";
  readonly resources: Resource;
}

export function createTestI18n(resources: Resource, locale = "en"): I18nInstance {
  const instance = i18next.createInstance();
  void instance
    .use(ICU)
    .use(pseudoLocalePostProcessor)
    .init({
      lng: locale,
      fallbackLng: "en",
      ns: [...localizationNamespaces],
      defaultNS: "common",
      resources,
      interpolation: { escapeValue: false },
      postProcess: [pseudoLocalePostProcessorName],
      react: { useSuspense: false },
    });
  return instance;
}

export function LocalizationTestProvider({
  children,
  locale = "en",
  direction = "ltr",
  resources,
}: LocalizationTestProviderProps) {
  const instance = React.useMemo(() => createTestI18n(resources, locale), [resources, locale]);

  React.useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = direction;
  }, [locale, direction]);

  return <I18nextProvider i18n={instance}>{children}</I18nextProvider>;
}

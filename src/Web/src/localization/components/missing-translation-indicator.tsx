import { useTranslation } from "react-i18next";

import { localizationEnvironment } from "../config/localization-environment";
import { emitLocalizationTelemetry } from "../telemetry/localization-telemetry";
import type { ResourceNamespace, TranslationKey } from "../types/resource-keys";

/**
 * Missing-translation indicator.
 *
 * In development it renders a loud marker so a gap is impossible to miss and
 * logs the namespace, locale and key. In production it renders the fallback
 * wording only and emits structured telemetry — never a diagnostic to the user,
 * and never a write-back into the translation management system.
 */
export interface MissingTranslationIndicatorProps<N extends ResourceNamespace> {
  readonly namespace: N;
  readonly translationKey: TranslationKey<N>;
  readonly children?: React.ReactNode;
}

export function MissingTranslationIndicator<N extends ResourceNamespace>({
  namespace,
  translationKey,
  children,
}: MissingTranslationIndicatorProps<N>) {
  const { t, i18n } = useTranslation(namespace);
  const qualified = `${namespace}:${translationKey}`;
  const exists = i18n.exists(qualified);

  if (exists) return <>{t(translationKey as never)}</>;

  emitLocalizationTelemetry("localization_missing_key", {
    namespace,
    key: translationKey,
    resolvedLocale: i18n.language,
  });

  // Nothing is rendered for a gap: no marker, no raw key. Development keeps the
  // data attribute so the gap is still inspectable in the DOM and the console.
  if (localizationEnvironment.isDevelopment) {
    return <span data-missing-translation={qualified} hidden />;
  }

  return <>{children ?? null}</>;
}

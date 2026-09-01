import * as React from "react";

import { useLocalization } from "../providers/localization-provider";

/** Active locale, direction and switching, without exposing the whole context. */
export function useLocale() {
  const { locale, direction, availableLocales, setLocale, releaseId, status } = useLocalization();
  return React.useMemo(
    () => ({ locale, direction, availableLocales, setLocale, releaseId, status }),
    [locale, direction, availableLocales, setLocale, releaseId, status],
  );
}

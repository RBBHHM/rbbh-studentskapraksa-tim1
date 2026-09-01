import * as React from "react";
import { useNavigate, useRouterState } from "@tanstack/react-router";

import { normalizeLocale } from "../config/locale-resolution";
import { useLocalization } from "../providers/localization-provider";

/**
 * Locale-aware navigation.
 *
 * Where locale-prefixed URLs are enabled (`/de/components`), switching the
 * language rewrites the current path in place instead of duplicating a route
 * component per language. Client navigation only — never a full page reload.
 */

export interface LocalizedNavigation {
  /** Path without a locale prefix, e.g. `/components`. */
  readonly basePath: string;
  /** Locale prefix currently present in the URL, if any. */
  readonly urlLocale: string | undefined;
  /** Build a path for a locale, preserving the current base path. */
  readonly buildLocalizedPath: (locale: string, path?: string) => string;
  /** Switch locale and keep the user on the same page. */
  readonly changeLocale: (locale: string) => void;
}

export function useLocalizedNavigation(): LocalizedNavigation {
  const navigate = useNavigate();
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const { manifest, setLocale } = useLocalization();

  return React.useMemo<LocalizedNavigation>(() => {
    const segments = pathname.split("/").filter(Boolean);
    const first = segments[0];
    const urlLocale = manifest && first ? normalizeLocale(first, manifest) : undefined;
    const hasPrefix = Boolean(urlLocale) && first === urlLocale;
    const basePath = `/${(hasPrefix ? segments.slice(1) : segments).join("/")}`;

    const buildLocalizedPath = (locale: string, path?: string) => {
      const target = path ?? basePath;
      const clean = target === "/" ? "" : target.replace(/^\/+/, "/");
      return hasPrefix ? `/${locale}${clean}` : clean || "/";
    };

    return {
      basePath,
      urlLocale,
      buildLocalizedPath,
      changeLocale: (locale: string) => {
        setLocale(locale);
        if (hasPrefix) void navigate({ to: buildLocalizedPath(locale), replace: true });
      },
    };
  }, [pathname, manifest, navigate, setLocale]);
}

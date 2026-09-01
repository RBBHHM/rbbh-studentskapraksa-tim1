import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";

/**
 * Theme = surface-context colour scheme, orthogonal to locale. Persisted in
 * localStorage; "system" resolves live from `prefers-color-scheme` /
 * `prefers-color-scheme` and keeps listening for OS changes.
 */
export type Theme = "light" | "dark" | "system";
export type ResolvedTheme = "light" | "dark";

const STORAGE_KEY = "rbi-theme";

/** Read the user's OS-level signal when no explicit choice has been stored. */
function resolveSystemTheme(): ResolvedTheme {
  if (typeof window === "undefined") return "light";
  if (window.matchMedia("(prefers-color-scheme: dark)").matches) return "dark";
  return "light";
}

function resolveTheme(theme: Theme): ResolvedTheme {
  return theme === "system" ? resolveSystemTheme() : theme;
}

/**
 * Inline, dependency-free script string. Runs in <head> before hydration so
 * `data-theme` is correct on first paint (no dark/light flash) and matches
 * what `ThemeProvider` computes on mount (no hydration warning).
 */
export const themeInitScript = `(function(){try{var s=localStorage.getItem("${STORAGE_KEY}");var t=s;if(t!=="light"&&t!=="dark"){t=window.matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light";}document.documentElement.setAttribute("data-theme",t);}catch(e){}})();`;

type ThemeContextValue = {
  /** Raw preference, including "system". */
  theme: Theme;
  /** The concrete theme actually applied to the document. */
  resolvedTheme: ResolvedTheme;
  setTheme: (theme: Theme) => void;
};

export const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>("system");
  const [resolvedTheme, setResolvedTheme] = useState<ResolvedTheme>("light");

  // Sync from localStorage once on mount — the inline head script already
  // applied `data-theme` to the DOM, so this only updates React state.
  useEffect(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    const initial: Theme =
      stored === "light" || stored === "dark" || stored === "system" ? stored : "system";
    setThemeState(initial);
    setResolvedTheme(resolveTheme(initial));
  }, []);

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", resolvedTheme);
  }, [resolvedTheme]);

  useEffect(() => {
    if (theme !== "system") return;
    const media = [window.matchMedia("(prefers-color-scheme: dark)")];
    const listener = () => setResolvedTheme(resolveSystemTheme());
    media.forEach((m) => m.addEventListener("change", listener));
    return () => media.forEach((m) => m.removeEventListener("change", listener));
  }, [theme]);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
    window.localStorage.setItem(STORAGE_KEY, next);
    setResolvedTheme(resolveTheme(next));
  }, []);

  const value = useMemo(
    () => ({ theme, resolvedTheme, setTheme }),
    [theme, resolvedTheme, setTheme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

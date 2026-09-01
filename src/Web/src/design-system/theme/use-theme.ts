import { useContext } from "react";

import { ThemeContext } from "./theme-provider";
import type { Theme, ResolvedTheme } from "./theme-provider";

export type { Theme, ResolvedTheme };

/** Access the current theme, its resolved value, and the setter. */
export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider");
  }
  return context;
}

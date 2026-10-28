(() => {
  const storageKey = "rbi-theme";
  const supported = new Set(["light", "dark", "system"]);
  const media = window.matchMedia("(prefers-color-scheme: dark)");

  function normalize(value) {
    return supported.has(value) ? value : "system";
  }

  function apply(preference) {
    const normalized = normalize(preference);
    const resolved = normalized === "system" ? (media.matches ? "dark" : "light") : normalized;

    document.documentElement.dataset.themePreference = normalized;
    document.documentElement.dataset.theme = resolved;
    return normalized;
  }

  function storedPreference() {
    try {
      return normalize(localStorage.getItem(storageKey));
    } catch {
      return "system";
    }
  }

  window.RbiTheme = {
    initialize() {
      return apply(storedPreference());
    },
    getPreference() {
      return storedPreference();
    },
    setPreference(preference) {
      const normalized = normalize(preference);
      try {
        localStorage.setItem(storageKey, normalized);
      } catch {}
      apply(normalized);
    },
  };

  media.addEventListener("change", () => {
    if (storedPreference() === "system") apply("system");
  });

  window.RbiTheme.initialize();
})();

/**
 * Localization loading state.
 *
 * Shown while the manifest and critical namespaces load. Deliberately free of
 * translation keys — the translations are exactly what is not available yet — so
 * it uses the neutral brand mark plus a `role="status"` announcement.
 */
export function LocalizationLoadingState({ label = "Loading…" }: { label?: string }) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex min-h-screen items-center justify-center bg-background px-4"
    >
      <div className="flex flex-col items-center gap-3">
        <span
          aria-hidden="true"
          className="size-6 animate-spin rounded-full border-2 border-border-subtle border-t-[color:var(--brand-yellow,currentColor)] motion-reduce:animate-none"
        />
        <span className="text-sm text-text-tertiary">{label}</span>
      </div>
    </div>
  );
}

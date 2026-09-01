/**
 * Controlled localization failure state.
 *
 * Reached only when neither the requested locale, the fallback chain nor a
 * validated last-known-good release is available. Shows a recovery action and
 * keeps technical diagnostics coarse: no URLs, no infrastructure details.
 */
export interface LocalizationErrorStateProps {
  readonly error: Error;
  readonly retry: () => void;
}

export function LocalizationErrorState({ error, retry }: LocalizationErrorStateProps) {
  return (
    <div
      role="alert"
      className="flex min-h-screen items-center justify-center bg-background px-4"
    >
      <div className="max-w-md text-center">
        <h1 className="text-xl font-semibold tracking-tight text-text-primary">
          Content could not be loaded
        </h1>
        <p className="mt-2 text-sm text-text-secondary">
          The application text is temporarily unavailable. Please try again in a moment.
        </p>
        <p className="mt-1 text-xs text-text-tertiary">Reference: {error.name}</p>
        <button
          type="button"
          onClick={retry}
          className="mt-6 inline-flex h-10 items-center justify-center rounded-sm border border-border-default px-4 text-sm font-medium text-text-primary transition-colors hover:border-border-strong focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[color:var(--focus-ring-color)]"
        >
          Try again
        </button>
      </div>
    </div>
  );
}

import { Suspense, type ReactNode } from "react";

export function RouteLoader({ children }: { readonly children: ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="grid min-h-48 place-items-center" role="status" aria-live="polite">
          <span className="text-sm text-text-secondary">Učitavanje…</span>
        </div>
      }
    >
      {children}
    </Suspense>
  );
}

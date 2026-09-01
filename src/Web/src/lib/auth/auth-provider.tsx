import { type ReactNode, useEffect, useState } from "react";
import { initializeAuthentication } from "./keycloak";

export function AuthProvider({ children }: { readonly children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<Error>();
  useEffect(() => {
    initializeAuthentication()
      .then(() => setReady(true))
      .catch((reason: unknown) =>
        setError(reason instanceof Error ? reason : new Error("Authentication failed.")),
      );
  }, []);
  if (error)
    return (
      <main className="flex min-h-screen items-center justify-center bg-background p-6">
        <div className="max-w-md rounded-sm border border-border-subtle bg-surface-default p-6">
          <h1 className="font-bold">Prijava trenutno nije dostupna / Sign-in is unavailable</h1>
          <p className="mt-2 text-sm text-text-secondary">{error.message}</p>
          <button className="mt-5 font-semibold underline" onClick={() => location.reload()}>
            Pokušaj ponovo / Try again
          </button>
        </div>
      </main>
    );
  if (!ready)
    return (
      <div role="status" className="flex min-h-screen items-center justify-center bg-background">
        <span className="text-sm text-text-secondary">
          Povezivanje sa sigurnom prijavom… / Connecting to secure sign-in…
        </span>
      </div>
    );
  return children;
}

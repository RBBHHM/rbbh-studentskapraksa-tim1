export interface CurrentUser {
  readonly userId?: string;
  readonly fullName?: string;
  readonly username?: string;
  readonly email?: string;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
}

let currentUser: CurrentUser | undefined;

export async function loadCurrentUser(): Promise<CurrentUser | undefined> {
  const response = await fetch("/api/auth/profile", {
    credentials: "include",
    headers: { accept: "application/json" },
  });
  if (response.status === 401) return undefined;
  if (!response.ok) throw new Error(`Authentication check failed (${response.status}).`);
  currentUser = await response.json() as CurrentUser;
  return currentUser;
}

export function getCurrentUser(): CurrentUser | undefined {
  return currentUser;
}
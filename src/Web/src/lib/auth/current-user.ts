export interface CurrentUser {
  readonly userId?: string;
  readonly fullName?: string;
  readonly username?: string;
  readonly email?: string;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
}

let currentUser: CurrentUser | undefined;

const developmentUserStorageKey = "rpr.development-user";

export function getDevelopmentUser(): string {
  return localStorage.getItem(developmentUserStorageKey) === "verifier1" ? "verifier1" : "admin1";
}

export function setDevelopmentUser(username: string): void {
  localStorage.setItem(developmentUserStorageKey, username === "verifier1" ? "verifier1" : "admin1");
}

export function developmentUserHeaders(): Record<string, string> {
  return { "x-development-user": getDevelopmentUser() };
}

export async function loadCurrentUser(): Promise<CurrentUser | undefined> {
  const response = await fetch("/api/auth/profile", {
    credentials: "include",
    headers: { accept: "application/json", ...developmentUserHeaders() },
  });
  if (response.status === 401) return undefined;
  if (!response.ok) throw new Error(`Authentication check failed (${response.status}).`);
  currentUser = await response.json() as CurrentUser;
  return currentUser;
}

export function getCurrentUser(): CurrentUser | undefined {
  return currentUser;
}

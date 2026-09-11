import { getCurrentUser, loadCurrentUser } from "./current-user";
import { redirect } from "@tanstack/react-router";

export const applicationAccessRoles = [
  "pl",
  "fl",
  "kapital",
  "limiti",
  "admin",
  "gost",
] as const;

export type ApplicationAccessRole = (typeof applicationAccessRoles)[number];

const modulePrefixes: Partial<Record<ApplicationAccessRole, string>> = {
  fl: "FL",
  pl: "PL",
  kapital: "KAPITAL",
  limiti: "LIMITI",
};

export function activeApplicationAccesses(): ReadonlySet<string> {
  return new Set((getCurrentUser()?.roles ?? []).map((role) => role.toLowerCase()));
}

export function hasApplicationAccess(role?: ApplicationAccessRole): boolean {
  if (!role) return true;
  if (role === "admin") return hasPermission("ADMINISTRATION_VIEW");
  if (role === "gost") return Boolean(getCurrentUser());
  return hasPermission(`${modulePrefixes[role]}_VIEW`);
}

export function canWriteApplicationAccess(role?: ApplicationAccessRole): boolean {
  const prefix = role ? modulePrefixes[role] : undefined;
  return Boolean(prefix && ["CREATE", "EDIT", "DELETE"].some((operation) =>
    hasPermission(`${prefix}_${operation}`)));
}

export function isApplicationAdmin(): boolean {
  return hasPermission("ADMINISTRATION_MANAGE");
}

export async function requireApplicationAdmin(): Promise<void> {
  if (!getCurrentUser()) await loadCurrentUser();
  if (!isApplicationAdmin()) throw redirect({ to: "/app" });
}

export function hasPermission(permission: string): boolean {
  return (getCurrentUser()?.permissions ?? []).some((item) =>
    item.localeCompare(permission, undefined, { sensitivity: "accent" }) === 0);
}

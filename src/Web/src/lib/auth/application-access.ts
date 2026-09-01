import { isAuthenticationConfigured, keycloak } from "./keycloak";

export const applicationAccessRoles = [
  "physical-persons",
  "legal-persons",
  "limits",
  "regulatory-reporting",
] as const;

export type ApplicationAccessRole = (typeof applicationAccessRoles)[number];

export function activeApplicationAccesses(): ReadonlySet<string> {
  if (!isAuthenticationConfigured) return new Set(applicationAccessRoles);
  const token = (keycloak.tokenParsed ?? {}) as Record<string, unknown>;
  const realm = token["realm_access"] as { roles?: string[] } | undefined;
  const resources = token["resource_access"] as Record<string, { roles?: string[] }> | undefined;
  return new Set([
    ...(realm?.roles ?? []),
    ...Object.values(resources ?? {}).flatMap((entry) => entry.roles ?? []),
  ].map((role) => role.toLowerCase()));
}

export function hasApplicationAccess(role?: ApplicationAccessRole): boolean {
  return !role || activeApplicationAccesses().has(role);
}

export function hasAllApplicationAccesses(): boolean {
  const active = activeApplicationAccesses();
  return applicationAccessRoles.every((role) => active.has(role));
}

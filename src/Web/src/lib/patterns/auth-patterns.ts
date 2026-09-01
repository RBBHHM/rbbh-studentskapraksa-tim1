/**
 * Auth & RBAC pattern data.
 *
 * The role guard illustration and the permission matrix are driven from this
 * single table so a new capability or role is a data change, never a JSX
 * change. All copy resolves through the `patterns` localization namespace.
 */

export const roles = ["viewer", "editor", "admin"] as const;
export type Role = (typeof roles)[number];

export interface PermissionRow {
  readonly capabilityKey: string;
  readonly grants: Readonly<Record<Role, boolean>>;
}

export const permissionMatrix: readonly PermissionRow[] = [
  { capabilityKey: "viewAccounts", grants: { viewer: true, editor: true, admin: true } },
  { capabilityKey: "exportReports", grants: { viewer: false, editor: true, admin: true } },
  { capabilityKey: "editRecords", grants: { viewer: false, editor: true, admin: true } },
  { capabilityKey: "manageUsers", grants: { viewer: false, editor: false, admin: true } },
  { capabilityKey: "changeRoles", grants: { viewer: false, editor: false, admin: true } },
];

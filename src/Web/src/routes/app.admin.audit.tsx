import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
import { requireApplicationAdmin } from "@/lib/auth/application-access";
const AuditPage = lazy(async () => {
  const [{ ResourcePage }, { resourcesByKey }] = await Promise.all([import("@/components/registry/resource-page"), import("@/lib/registry/resources")]);
  return { default: () => <ResourcePage resource={resourcesByKey.get("audit")!} /> };
});
export const Route = createFileRoute("/app/admin/audit")({
  beforeLoad: requireApplicationAdmin,
  component: () => <RouteLoader><AuditPage /></RouteLoader>,
});

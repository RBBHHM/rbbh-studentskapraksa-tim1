import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const LimitsPage = lazy(async () => {
  const [{ ResourcePage }, { ExportButton }, { resourcesByKey }] = await Promise.all([import("@/components/registry/resource-page"), import("@/components/registry/export-button"), import("@/lib/registry/resources")]);
  return { default: () => <ResourcePage resource={resourcesByKey.get("limits")!} toolbar={<ExportButton endpoint="/api/limiti/export" fileName="limiti.xlsx" />} /> };
});
export const Route = createFileRoute("/app/limits")({
  component: () => <RouteLoader><LimitsPage /></RouteLoader>,
});

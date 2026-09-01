import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const PhysicalPersonsPage = lazy(async () => {
  const [{ ResourcePage }, { ImportPanel }, { ExportButton }, { FamilyManager }, { resourcesByKey }] = await Promise.all([
    import("@/components/registry/resource-page"),
    import("@/components/registry/import-panel"),
    import("@/components/registry/export-button"),
    import("@/components/registry/family-manager"),
    import("@/lib/registry/resources"),
  ]);
  return { default: () => <><ResourcePage resource={resourcesByKey.get("physicalPersons")!} toolbar={<><ImportPanel endpoint="/api/related-persons/import" /><ExportButton endpoint="/api/related-persons/export" fileName="fizicka-lica.xlsx" /></>} /><FamilyManager /></> };
});
export const Route = createFileRoute("/app/physical-persons")({
  component: () => <RouteLoader><PhysicalPersonsPage /></RouteLoader>,
});

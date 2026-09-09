import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const PhysicalPersonsPage = lazy(async () => {
  const [{ ResourcePage }, { ImportPanel }, { ExportButton }, { FamilyManager }, { resourcesByKey }, { canWriteApplicationAccess }] = await Promise.all([
    import("@/components/registry/resource-page"),
    import("@/components/registry/import-panel"),
    import("@/components/registry/export-button"),
    import("@/components/registry/family-manager"),
    import("@/lib/registry/resources"),
    import("@/lib/auth/application-access"),
  ]);
  return { default: () => <><ResourcePage resource={resourcesByKey.get("physicalPersons")!} toolbar={<>{canWriteApplicationAccess("fl") ? <ImportPanel endpoint="/api/related-persons/import" /> : null}<ExportButton endpoint="/api/related-persons/export" fileName="fizicka-lica.xlsx" /></>} /><FamilyManager /></> };
});
export const Route = createFileRoute("/app/physical-persons")({
  component: () => <RouteLoader><PhysicalPersonsPage /></RouteLoader>,
});

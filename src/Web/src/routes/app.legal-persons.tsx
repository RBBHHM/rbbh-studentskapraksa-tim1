import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const LegalPersonsPage = lazy(async () => {
  const [{ ResourcePage }, { ImportPanel }, { ExportButton }, { resourcesByKey }, { canWriteApplicationAccess }] = await Promise.all([
    import("@/components/registry/resource-page"),
    import("@/components/registry/import-panel"),
    import("@/components/registry/export-button"),
    import("@/lib/registry/resources"),
    import("@/lib/auth/application-access"),
  ]);
  return { default: () => <ResourcePage resource={resourcesByKey.get("legalPersons")!} toolbar={<>{canWriteApplicationAccess("pl") ? <ImportPanel endpoint="/api/legal-entities/import" /> : null}<ExportButton endpoint="/api/legal-entities/export" fileName="pravna-lica.xlsx" /></>} /> };
});
export const Route = createFileRoute("/app/legal-persons")({
  component: () => <RouteLoader><LegalPersonsPage /></RouteLoader>,
});

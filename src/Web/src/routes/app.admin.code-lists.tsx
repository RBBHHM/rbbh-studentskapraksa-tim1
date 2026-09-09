import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
import { requireApplicationAdmin } from "@/lib/auth/application-access";
const CodeListsPage = lazy(() => import("@/components/registry/code-lists-page").then((module) => ({ default: module.CodeListsPage })));
export const Route = createFileRoute("/app/admin/code-lists")({
  beforeLoad: requireApplicationAdmin,
  component: () => <RouteLoader><CodeListsPage /></RouteLoader>,
});

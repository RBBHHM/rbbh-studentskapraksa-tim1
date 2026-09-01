import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const CodeListsPage = lazy(() => import("@/components/registry/code-lists-page").then((module) => ({ default: module.CodeListsPage })));
export const Route = createFileRoute("/app/admin/code-lists")({
  component: () => <RouteLoader><CodeListsPage /></RouteLoader>,
});

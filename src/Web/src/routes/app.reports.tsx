import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const ReportsPage = lazy(() => import("@/components/registry/reports-page").then((module) => ({ default: module.ReportsPage })));
export const Route = createFileRoute("/app/reports")({
  component: () => <RouteLoader><ReportsPage /></RouteLoader>,
});

import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const PeriodPage = lazy(() => import("@/components/registry/period-page").then((module) => ({ default: module.PeriodPage })));
export const Route = createFileRoute("/app/admin/period")({
  component: () => <RouteLoader><PeriodPage /></RouteLoader>,
});

import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
import { requireApplicationAdmin } from "@/lib/auth/application-access";
const PeriodPage = lazy(() => import("@/components/registry/period-page").then((module) => ({ default: module.PeriodPage })));
export const Route = createFileRoute("/app/admin/period")({
  beforeLoad: requireApplicationAdmin,
  component: () => <RouteLoader><PeriodPage /></RouteLoader>,
});

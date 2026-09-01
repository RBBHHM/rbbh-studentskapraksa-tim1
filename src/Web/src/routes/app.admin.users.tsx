import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const UsersPage = lazy(() => import("@/components/registry/users-page").then((module) => ({ default: module.UsersPage })));
export const Route = createFileRoute("/app/admin/users")({
  component: () => <RouteLoader><UsersPage /></RouteLoader>,
});

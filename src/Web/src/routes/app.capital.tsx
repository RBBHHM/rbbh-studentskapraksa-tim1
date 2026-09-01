import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const CapitalPage = lazy(() => import("@/components/registry/capital-page").then((module) => ({ default: module.CapitalPage })));
export const Route = createFileRoute("/app/capital")({ component: () => <RouteLoader><CapitalPage /></RouteLoader> });

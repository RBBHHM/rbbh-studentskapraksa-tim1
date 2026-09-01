import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";

import { RouteLoader } from "@/components/registry/route-loader";

const UserGuidePage = lazy(() => import("@/components/registry/user-guide-page").then((module) => ({ default: module.UserGuidePage })));

export const Route = createFileRoute("/app/guide")({ component: () => <RouteLoader><UserGuidePage /></RouteLoader> });

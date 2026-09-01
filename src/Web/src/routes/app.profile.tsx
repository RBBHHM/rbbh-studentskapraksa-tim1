import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const ProfilePage = lazy(() => import("@/components/registry/profile-page").then((module) => ({ default: module.ProfilePage })));

export const Route = createFileRoute("/app/profile")({ component: () => <RouteLoader><ProfilePage /></RouteLoader> });

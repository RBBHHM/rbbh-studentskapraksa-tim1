import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";
import { RouteLoader } from "@/components/registry/route-loader";
const NotificationsPage = lazy(async () => {
  const [{ ResourcePage }, { resourcesByKey }] = await Promise.all([import("@/components/registry/resource-page"), import("@/lib/registry/resources")]);
  return { default: () => <ResourcePage resource={resourcesByKey.get("notifications")!} /> };
});
export const Route = createFileRoute("/app/notifications")({
  component: () => <RouteLoader><NotificationsPage /></RouteLoader>,
});

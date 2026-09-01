import { createFileRoute } from "@tanstack/react-router";

import { RegistryShell } from "@/components/registry/registry-shell";

export const Route = createFileRoute("/app")({ component: RegistryShell });

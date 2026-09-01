import { Boxes, Database, ServerCog, Sparkles, type LucideIcon } from "lucide-react";

export interface FolderNode {
  readonly key: string;
  readonly path: string;
  readonly depth: 0 | 1 | 2;
}

export const folders: readonly FolderNode[] = [
  { key: "program", path: "Program.cs", depth: 0 },
  { key: "server", path: "Server/", depth: 0 },
  { key: "serverContracts", path: "Contracts/ · Configuration/", depth: 1 },
  { key: "serverInfrastructure", path: "Http/ · Authentication/ · Middleware/", depth: 1 },
  { key: "serverExamples", path: "Examples/", depth: 1 },
  { key: "routesDir", path: "src/routes/", depth: 0 },
  { key: "components", path: "src/components/", depth: 0 },
  { key: "ui", path: "ui/ · layout/ · brand/", depth: 1 },
  { key: "lib", path: "src/lib/", depth: 0 },
  { key: "libApi", path: "api/ · api/generated/", depth: 1 },
  { key: "localization", path: "src/localization/", depth: 0 },
  { key: "designSystem", path: "src/design-system/ · src/styles.css", depth: 0 },
  { key: "docs", path: "docs/", depth: 0 },
];

export interface PipelineNode {
  readonly key: string;
  readonly icon: LucideIcon;
  readonly file: string;
  readonly side: "browser" | "server" | "service";
}

export const pipelineNodes: readonly PipelineNode[] = [
  { key: "screen", icon: Sparkles, file: "src/routes/applications.api.tsx", side: "browser" },
  { key: "client", icon: Boxes, file: "src/lib/api/example-client.ts", side: "browser" },
  {
    key: "route",
    icon: ServerCog,
    file: "Server/Examples/AccountExampleEndpoints.cs",
    side: "server",
  },
  { key: "service", icon: Database, file: "Server/Http/ExternalApiClient.cs", side: "service" },
];

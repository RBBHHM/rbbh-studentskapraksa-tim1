import { Database, Globe, type LucideIcon } from "lucide-react";

export type FlowZone = "browser" | "server" | "service";
export interface FlowStep {
  readonly id: string;
  readonly zone: FlowZone;
  readonly file: string;
  readonly snippet: string;
}
export interface Flow {
  readonly id: string;
  readonly icon: LucideIcon;
  readonly steps: readonly FlowStep[];
}
export const zoneKeys: readonly FlowZone[] = ["browser", "server", "service"];
export const zoneBorders: Record<FlowZone, string> = {
  browser: "border-t-border-brand",
  server: "border-t-border-strong",
  service: "border-t-border-default",
};

export const flows: readonly Flow[] = [
  {
    id: "read",
    icon: Globe,
    steps: [
      {
        id: "read-route",
        zone: "browser",
        file: "src/routes/applications.api.tsx",
        snippet: "useQuery({ queryFn: () => exampleClient.listAccounts(query) })",
      },
      {
        id: "read-client",
        zone: "browser",
        file: "src/lib/api/example-client.ts",
        snippet: "return apiClient.get<AccountPage>(`/api/public/accounts?${params}`);",
      },
      {
        id: "read-handler",
        zone: "server",
        file: "Server/Examples/AccountExampleEndpoints.cs",
        snippet: 'group.MapGet("/accounts", (...) => Results.Ok(ApiResponse.Ok(page)));',
      },
      {
        id: "read-source",
        zone: "service",
        file: "Server/Http/ExternalApiClient.cs",
        snippet: "return await httpClient.GetFromJsonAsync<T>(path, cancellationToken);",
      },
    ],
  },
  {
    id: "write",
    icon: Database,
    steps: [
      {
        id: "write-form",
        zone: "browser",
        file: "src/components/admin/account-form-dialog.tsx",
        snippet:
          "const result = validateDraft(draft);\nif (result.ok) mutation.mutate(result.value);",
      },
      {
        id: "write-reducer",
        zone: "browser",
        file: "src/lib/api/example-client.ts",
        snippet: 'return apiClient.post<Account>("/api/example/accounts", draft);',
      },
      {
        id: "write-derived",
        zone: "server",
        file: "Server/Examples/AccountExampleEndpoints.cs",
        snippet: 'group.MapPost("/accounts", (AccountDraft request) => ...);',
      },
      {
        id: "write-swap",
        zone: "service",
        file: "Server/Http/ExternalApiClient.cs",
        snippet: "await externalApi.PostAsJsonAsync(path, request, cancellationToken);",
      },
    ],
  },
];

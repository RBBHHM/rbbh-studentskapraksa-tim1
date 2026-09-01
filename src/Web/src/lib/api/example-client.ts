import { apiClient } from "@/lib/api/http-client";
import type { components } from "@/lib/api/generated/api";

/** Neutral reference client using the generated OpenAPI contract directly. */
export type ExampleResponse = components["schemas"]["ExampleResponse"];

export function fetchExample(signal?: AbortSignal) {
  return apiClient.get<ExampleResponse>("/api/example", { signal });
}

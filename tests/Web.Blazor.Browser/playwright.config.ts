import { defineConfig } from "@playwright/test";

const url = process.env.BROWSER_FIXTURE_URL ?? "http://127.0.0.1:5273";
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
export default defineConfig({
  testDir: ".", testMatch: role === "gost" ? ["read-only.spec.ts", "legal-read-only.spec.ts", "limits-read-only.spec.ts", "capital-read-only.spec.ts"] : role === "PL" ? "legal-entities.spec.ts" : role === "limiti" ? "limits.spec.ts" : role === "kapital" || role === "admin" ? "capital.spec.ts" : "physical-persons.spec.ts", workers: 1, retries: 0,
  use: { baseURL: url, browserName: "chromium", locale: "bs-BA", viewport: { width: 1440, height: 1000 }, trace: "retain-on-failure" },
  outputDir: `../../artifacts/migration-browser/${role}/results`,
  reporter: [["list"], ["json", { outputFile: `../../artifacts/migration-browser/${role}/results.json` }]],
  webServer: {
    command: "dotnet run --project ../Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj --no-build --no-restore --no-launch-profile",
    url: `${url}/app`, reuseExistingServer: false,
  },
});

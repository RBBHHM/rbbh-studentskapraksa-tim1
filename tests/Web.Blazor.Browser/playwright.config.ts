import { defineConfig } from "@playwright/test";

const url = process.env.BROWSER_FIXTURE_URL ?? "http://127.0.0.1:5273";
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
const roleTests = role === "gost" ? ["read-only.spec.ts", "legal-read-only.spec.ts", "limits-read-only.spec.ts", "capital-read-only.spec.ts", "reports-read-only.spec.ts"]
  : role === "PL" ? ["legal-entities.spec.ts"] : role === "limiti" ? ["limits.spec.ts"]
  : role === "kapital" || role === "admin" ? ["capital.spec.ts", "reports.spec.ts"] : ["physical-persons.spec.ts"];
export default defineConfig({
  testDir: ".", testMatch: ["shell-profile-guide.spec.ts", "notifications.spec.ts", ...(role === "admin" ? ["audit.spec.ts", "users-safety.spec.ts", "users.spec.ts", "period.spec.ts", "code-lists.spec.ts"] : ["audit-denied.spec.ts", "users-denied.spec.ts", "period-denied.spec.ts", "code-lists-denied.spec.ts"]), ...roleTests], workers: 1, retries: 0,
  use: { baseURL: url, browserName: "chromium", locale: "bs-BA", viewport: { width: 1440, height: 1000 }, trace: "retain-on-failure" },
  outputDir: `../../artifacts/migration-browser/${role}/results`,
  reporter: [["list"], ["json", { outputFile: `../../artifacts/migration-browser/${role}/results.json` }]],
  webServer: {
    command: `dotnet run --project ../Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj ${process.env.BROWSER_SAFETY_BUILD === "true" ? "--artifacts-path ../../artifacts/users-safety" : ""} --no-build --no-restore --no-launch-profile`,
    url: `${url}/app`, reuseExistingServer: false,
  },
});

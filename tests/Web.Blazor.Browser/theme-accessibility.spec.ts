import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const admin = (process.env.BROWSER_FIXTURE_ROLE ?? "FL") === "admin";
const routes = ["/app", "/app/profile", "/app/guide", "/app/notifications", ...(admin ? ["/app/admin/users", "/app/admin/period", "/app/admin/code-lists", "/app/admin/audit", "/app/capital", "/app/limits", "/app/legal-persons", "/app/physical-persons", "/app/reports"] : [])];
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
for (const scheme of ["light", "dark"] as const) for (const route of routes) {
  test(`Axe finds no WCAG A/AA violations on ${route} in ${scheme} mode`, async ({ page }) => {
    await page.emulateMedia({ colorScheme: scheme });
    await page.goto(route); await interactive(page);
    await expect(page.locator("html")).toHaveAttribute("data-theme", scheme);
    const result = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze();
    expect(result.violations.map(item => `${item.id}: ${item.nodes.length}`)).toEqual([]);
  });
}

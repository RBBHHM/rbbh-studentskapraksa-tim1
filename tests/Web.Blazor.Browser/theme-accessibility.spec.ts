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
for (const scheme of ["light", "dark"] as const) for (const route of routes) {
  test(`keyboard Tab order has no trap and every stop shows a focus indicator on ${route} in ${scheme} mode`, async ({ page }) => {
    await page.emulateMedia({ colorScheme: scheme });
    await page.goto(route); await interactive(page);
    const seen = new Set<string>(); const hidden: string[] = [];
    for (let step = 0; step < 90; step++) {
      await page.keyboard.press("Tab");
      const stop = await page.evaluate(() => {
        const element = document.activeElement as HTMLElement | null;
        if (!element || element === document.body) return null;
        const style = getComputedStyle(element);
        const ring = (style.outlineStyle !== "none" && parseFloat(style.outlineWidth) > 0) || (style.boxShadow !== "none" && style.boxShadow !== "");
        const parentRing = element.closest("label, summary, [role=option], .registry-search") ? (() => { const parent = getComputedStyle(element.closest("label, summary, [role=option], .registry-search") as HTMLElement); return (parent.outlineStyle !== "none" && parseFloat(parent.outlineWidth) > 0) || parent.boxShadow !== "none"; })() : false;
        const box = element.getBoundingClientRect();
        return { id: `${element.tagName}|${element.id}|${element.getAttribute("href") ?? ""}|${(element.getAttribute("aria-label") ?? element.textContent ?? "").trim().slice(0, 30)}|${Math.round(box.x)}|${Math.round(box.y)}`, visible: ring || parentRing };
      });
      if (stop === null) continue; // end of document: the browser wraps to the top
      if (!stop.visible) hidden.push(stop.id);
      seen.add(stop.id);
    }
    expect(seen.size).toBeGreaterThan(3);
    expect(hidden).toEqual([]);
  });
}

import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
test("database Admin reads the real empty audit contract without mutation controls", async ({ page, request }) => {
  await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByText("Nema revizijskih zapisa za prikaz.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/audit-status", { headers })).json()).status).toBe(200);
  for (const name of ["Novi zapis", "Uredi", "Obriši"]) await expect(page.getByRole("button", { name, exact: true })).toHaveCount(0);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("real audit entries retain human fields and scalar search without executing markup", async ({ page, request }) => {
  await request.post("/__fixtures/audit-existing", { headers }); await page.goto("/app/admin/audit"); await interactive(page);
  const row = page.getByRole("row", { name: /fixture-auditor/ }); await expect(row).toBeVisible();
  await expect(row.getByRole("cell", { name: "Unos", exact: true })).toBeVisible();
  await expect(row.getByRole("cell", { name: "Fizička lica", exact: true })).toBeVisible();
  await page.getByLabel("Pretraga podataka na prikazanoj stranici", { exact: true }).fill("  audit-body-needle  "); await expect(row).toBeVisible();
  expect(await page.evaluate(() => (window as unknown as Record<string, unknown>).__auditExecuted)).toBeUndefined();
  await expect(page.locator('img[src="x"]')).toHaveCount(0);
  await page.getByLabel("Pretraga podataka na prikazanoj stranici", { exact: true }).fill("no-match"); await expect(row).toHaveCount(0);
});
test("audit paging reaches records beyond the source first 200", async ({ page, request }) => {
  await request.post("/__fixtures/audit-pages", { headers }); await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByRole("row", { name: /audit-page-000/ })).toBeVisible(); await expect(page.getByRole("row", { name: /audit-page-200/ })).toHaveCount(0);
  await page.getByRole("button", { name: "Sljedeća stranica", exact: true }).click();
  await expect(page.getByRole("row", { name: /audit-page-200/ })).toBeVisible(); await expect(page.getByRole("row", { name: /audit-page-000/ })).toHaveCount(0);
});
test("audit outage fails safely and recovers without synthetic entries", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible();
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByText("Nema revizijskih zapisa za prikaz.", { exact: true })).toBeVisible();
});
test("375px audit view and BS/EN backend display copy remain usable", async ({ page, request }, info) => {
  await request.post("/__fixtures/audit-existing", { headers }); await page.setViewportSize({ width: 375, height: 812 }); await page.goto("/app/admin/audit"); await interactive(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true); await page.screenshot({ path: info.outputPath("audit-mobile.png"), fullPage: true });
  await page.getByRole("button", { name: "Prikaži ili sakrij navigaciju", exact: true }).click();
  const navigation = page.getByRole("dialog"); await expect(navigation).toBeVisible();
  await chooseLanguage(navigation, "en");

  await expect(page.getByRole("heading", { name: "Audit log", exact: true })).toBeVisible();
  const row = page.getByRole("row", { name: /fixture-auditor/ }); await expect(row.getByRole("cell", { name: "Create", exact: true })).toBeVisible();
  await expect(row.getByRole("cell", { name: "Individuals", exact: true })).toBeVisible();
});

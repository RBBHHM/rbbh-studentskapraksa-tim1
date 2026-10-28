import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
const admin = role === "admin";
async function visit(page: Page, route: string) { await page.goto(route); await interactive(page); }
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
for (const state of ["inactive"]) test(`Reports denies ${state} authenticated identity despite forged provider Admin`, async ({ page, request }) => {
  expect((await request.post(`/__fixtures/identity-${state}`, { headers })).status()).toBe(204);
  expect((await request.post("/__fixtures/forge-provider-admin", { headers })).status()).toBe(204);
  expect((await (await request.get("/__fixtures/front-identity", { headers })).json()).providerAdmin).toBe(true);
  const matrix = await (await request.post("/__fixtures/authorization-matrix", { headers })).json();
  expect(matrix.reportRead).toBe(403);
  await visit(page, "/app"); await expect(page.locator("a[href='/app/reports']")).toHaveCount(0);
  await visit(page, "/app/reports"); await expect(page.locator("nav a[href='/app/reports']")).toHaveCount(0);
  await expect(page.getByRole("main").getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: /Preuzmi|Generiši|Svi klijenti/ })).toHaveCount(0);
  expect((await (await request.get("/__fixtures/reports", { headers })).json()).records).toEqual([]);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(0);
});
test("unmapped authenticated identity gets virtual Guest reads but no forged Admin writes", async ({ page, request }) => {
  await request.post("/__fixtures/identity-unknown", { headers });
  await request.post("/__fixtures/forge-provider-admin", { headers });
  const matrix = await (await request.post("/__fixtures/authorization-matrix", { headers })).json();
  expect(matrix.reportRead).toBe(200);
  for (const action of ["physical", "legal", "limits", "capital", "generate", "users", "codes"]) expect(matrix[action]).toBe(403);
  await visit(page, "/app");
  await expect(page.getByRole("main").locator("a[href='/app/reports']")).toBeVisible();
  await expect(page.locator("a[href='/app/admin/users']")).toHaveCount(0);
  await visit(page, "/app/reports");
  await expect(page.getByRole("button", { name: /Generiši/ })).toHaveCount(0);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(0);
});
test("dashboard and navigation reflect actual DB baseline reads and privileged Admin modules", async ({ page, request }) => {
  await request.post("/__fixtures/forge-provider-admin", { headers }); await visit(page, "/app");
  const main = page.getByRole("main");
  for (const path of ["physical-persons", "legal-persons", "limits", "capital", "reports", "notifications"]) await expect(main.locator(`a[href='/app/${path}']`)).toBeVisible();
  // Profile, guide and administration are shell navigation (as in the reference product), not dashboard cards.
  for (const path of ["profile", "guide"]) await expect(page.locator(`a[href='/app/${path}']`).first()).toBeVisible();
  for (const path of ["users", "period", "code-lists", "audit"]) await expect(page.locator(`aside a[href='/app/admin/${path}']`)).toHaveCount(admin ? 1 : 0);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("template profile displays authenticated identity and current backend-owned grants, not forged provider roles", async ({ page, request }) => {
  await request.post("/__fixtures/forge-provider-admin", { headers }); await visit(page, "/app/profile");
  const main = page.getByRole("main"); await expect(main.getByText(role + "@fixture.test", { exact: true }).first()).toBeVisible();
  await expect(main.getByText("Trenutno prijavljeni korisnik", { exact: true })).toBeVisible(); await expect(main.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(main.locator("dt").filter({ hasText: "Efektivna ovlaštenja" }).locator("+ dd")).toContainText("FL_VIEW");
  const grants = await main.locator("dl").innerText(); expect(grants.includes("ADMINISTRATION_MANAGE")).toBe(admin);
  if (!admin) expect(await main.locator("dt").filter({ hasText: /^Dodijeljene uloge$/ }).locator("+ dd").innerText()).not.toMatch(/\badmin\b/i);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("permission-service outage invents no grants or guide sections and explicit retry recovers", async ({ page, request }) => {
  await request.post("/__fixtures/profile-unavailable", { headers }); await visit(page, "/app/profile");
  await expect(page.getByText("Aplikacijske dozvole trenutno nisu dostupne.", { exact: true })).toBeVisible(); await expect(page.getByRole("main").getByText("Efektivna ovlaštenja", { exact: true })).toHaveCount(0);
  await expect(page.getByText(/private-fixture/)).toHaveCount(0); expect((await request.post("/__fixtures/profile-available", { headers })).status()).toBe(204); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.getByRole("main").getByText("Efektivna ovlaštenja", { exact: true })).toBeVisible();
  await request.post("/__fixtures/profile-unavailable", { headers }); await visit(page, "/app/guide"); await expect(page.locator("details")).toHaveCount(0); await expect(page.getByText("Aplikacijske dozvole trenutno nisu dostupne.", { exact: true })).toBeVisible();
  expect((await request.post("/__fixtures/profile-available", { headers })).status()).toBe(204); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.locator("details")).toHaveCount(admin ? 5 : 4);
});
test("business guide is permission-aware, keyboard accessible and documents approved reporting distinction", async ({ page, request }) => {
  await request.post("/__fixtures/forge-provider-admin", { headers }); await visit(page, "/app/guide"); await expect(page.locator("details")).toHaveCount(admin ? 5 : 4);
  await expect(page.locator("summary").filter({ hasText: "Administracija" })).toHaveCount(admin ? 1 : 0);
  const first = page.locator("details").first(); await expect(first).toHaveAttribute("open", ""); await first.locator("summary").focus(); await page.keyboard.press("Space"); await expect(first).not.toHaveAttribute("open", ""); await page.keyboard.press("Enter"); await expect(first).toHaveAttribute("open", "");
  const reporting = page.locator("details").filter({ hasText: "Generisanje, preuzimanje i kontrola izvještajnog perioda." }); await reporting.locator("summary").click();
  await expect(reporting.getByText("Autentificirani korisnici koji nisu deaktivirani mogu pregledati i preuzimati postojeće izvještaje. Bez eksplicitne aplikacijske role efektivni pristup je Guest.", { exact: true })).toBeVisible(); await expect(reporting.getByText(/Nije read-only radnja/)).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("dashboard/profile/guide remain readable at375px without page overflow", async ({ page }, info) => {
  await page.setViewportSize({ width: 375, height: 812 });
  for (const path of ["/app", "/app/profile", "/app/guide"]) {
    await visit(page, path); await expect(page.getByRole("main")).toBeVisible(); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: info.outputPath(path === "/app" ? "dashboard-mobile.png" : path.split("/").at(-1) + "-mobile.png"), fullPage: true });
  }
});
test("BS/EN resources, culture cookie and backend language remain consistent across guide/profile/home", async ({ page, request }) => {
  await visit(page, "/app/guide"); await chooseLanguage(page, "en"); await expect(page.getByRole("heading", { name: "User guide", exact: true })).toBeVisible(); await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.getByRole("option", { name: /Deutsch|German/ })).toHaveCount(0); await page.reload(); await interactive(page); await expect(page.getByRole("heading", { name: "User guide", exact: true })).toBeVisible();
  await page.getByRole("link", { name: "My profile", exact: true }).click(); await expect(page.getByRole("main").getByText("Effective permissions", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
  await page.getByRole("link", { name: "Overview", exact: true }).click(); await expect(page.getByRole("main").getByRole("link", { name: "Open Individuals", exact: true })).toBeVisible();
});

test("dark guide retains WCAG AA contrast at desktop and375px", async ({ page }) => {
  for (const width of [1440, 375]) {
    await page.setViewportSize({ width, height: width === 375 ? 812 : 900 });
    await visit(page, "/app/guide");
    await page.getByRole("button", { name: "Tamna", exact: true }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  }
});

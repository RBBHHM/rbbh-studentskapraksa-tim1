import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
const admin = role === "admin";
async function visit(page: Page, route: string) { await page.goto(route); await interactive(page); }
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
test("dashboard and navigation reflect actual DB baseline reads and privileged Admin modules", async ({ page, request }) => {
  await request.post("/__fixtures/forge-provider-admin", { headers }); await visit(page, "/app");
  const main = page.getByRole("main");
  for (const path of ["physical-persons", "legal-persons", "limits", "capital", "reports", "notifications", "profile", "guide"]) await expect(main.locator(`a[href='/app/${path}']`)).toBeVisible();
  for (const path of ["users", "period", "code-lists", "audit"]) await expect(main.locator(`a[href='/app/admin/${path}']`)).toHaveCount(admin ? 1 : 0);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("template profile displays authenticated identity and current backend-owned grants, not forged provider roles", async ({ page, request }) => {
  await request.post("/__fixtures/forge-provider-admin", { headers }); await visit(page, "/app/profile");
  const main = page.getByRole("main"); await expect(main.getByText(role + "@fixture.test", { exact: true }).first()).toBeVisible();
  await expect(main.getByText("fixture-user", { exact: true })).toBeVisible(); await expect(main.getByText("Korisničko ime", { exact: true })).toBeVisible();
  await expect(main.locator("dt").filter({ hasText: "Aplikacijske dozvole" }).locator("+ dd")).toContainText("FL_VIEW");
  const grants = await main.locator("dl").innerText(); expect(grants.includes("ADMINISTRATION_MANAGE")).toBe(admin);
  if (!admin) expect(await main.locator("dt").filter({ hasText: /^Uloge$/ }).locator("+ dd").innerText()).not.toMatch(/\badmin\b/i);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("permission-service outage invents no grants or guide sections and explicit retry recovers", async ({ page, request }) => {
  await request.post("/__fixtures/profile-unavailable", { headers }); await visit(page, "/app/profile");
  await expect(page.getByText("Aplikacijske dozvole trenutno nisu dostupne.", { exact: true })).toBeVisible(); await expect(page.getByRole("main").getByText("Aplikacijske dozvole", { exact: true })).toHaveCount(0);
  await expect(page.getByText(/private-fixture/)).toHaveCount(0); expect((await request.post("/__fixtures/profile-available", { headers })).status()).toBe(204); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.getByRole("main").getByText("Aplikacijske dozvole", { exact: true })).toBeVisible();
  await request.post("/__fixtures/profile-unavailable", { headers }); await visit(page, "/app/guide"); await expect(page.locator("details")).toHaveCount(0); await expect(page.getByText("Aplikacijske dozvole trenutno nisu dostupne.", { exact: true })).toBeVisible();
  expect((await request.post("/__fixtures/profile-available", { headers })).status()).toBe(204); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.locator("details")).toHaveCount(admin ? 7 : 6);
});
test("business guide is permission-aware, keyboard accessible and documents approved reporting distinction", async ({ page, request }) => {
  await request.post("/__fixtures/forge-provider-admin", { headers }); await visit(page, "/app/guide"); await expect(page.locator("details")).toHaveCount(admin ? 7 : 6);
  await expect(page.locator("summary").filter({ hasText: "Administracija" })).toHaveCount(admin ? 1 : 0);
  const first = page.locator("details").first(); await expect(first).toHaveAttribute("open", ""); await first.locator("summary").focus(); await page.keyboard.press("Space"); await expect(first).not.toHaveAttribute("open", ""); await page.keyboard.press("Enter"); await expect(first).toHaveAttribute("open", "");
  const reporting = page.locator("details").filter({ hasText: "Arhiva, preuzimanje i generisanje izvještaja." }); await reporting.locator("summary").click();
  await expect(reporting.getByText("Svi autentificirani korisnici mogu pregledati i preuzimati postojeće izvještaje.", { exact: true })).toBeVisible(); await expect(reporting.getByText(/Nije read-only radnja/)).toBeVisible();
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
  await visit(page, "/app/guide"); await page.getByLabel("Jezik", { exact: true }).selectOption("en"); await page.getByRole("button", { name: "Primijeni", exact: true }).click(); await interactive(page); await expect(page.getByRole("heading", { name: "User guide", exact: true })).toBeVisible(); await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.getByRole("option", { name: /Deutsch|German/ })).toHaveCount(0); await page.reload(); await interactive(page); await expect(page.getByRole("heading", { name: "User guide", exact: true })).toBeVisible();
  await page.getByRole("link", { name: "My profile", exact: true }).click(); await expect(page.getByRole("main").getByText("Application permissions", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
  await page.getByRole("link", { name: "Home", exact: true }).click(); await expect(page.getByRole("main").locator("a[href='/app/guide']")).toHaveText("User guide");
});

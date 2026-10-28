import { expect, test, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
import { readFile } from "node:fs/promises";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => {
  for (const action of ["reset", "legal-existing", "report-reference"]) expect((await request.post(`/__fixtures/${action}`, { headers })).status()).toBe(204);
});
async function open(page: Page) { await page.goto("/app/reports"); await interactive(page); }
test("empty archive, labelled generation controls and authenticated read tools are accessible", async ({ page }) => {
  await open(page); await expect(page.getByText("Još nema generisanih izvještaja za prikaz.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Generiši dnevni", exact: true })).toBeVisible();
  await expect(page.getByLabel("Godina", { exact: true })).toHaveAttribute("min", "2000");
  await expect(page.getByLabel("Godina", { exact: true })).toHaveAttribute("max", String(new Date().getUTCFullYear()));
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("daily generation persists real snapshot and automatically downloads without changing period scope", async ({ page, request }, info) => {
  await request.post("/__fixtures/lock", { headers }); await open(page);
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Generiši dnevni", exact: true }).click();
  const download = await pending; expect(download.suggestedFilename()).toMatch(/^izvjestaj_.*\.xlsx$/);
  const target = info.outputPath("daily.xlsx"); await download.saveAs(target); expect((await readFile(target)).subarray(0, 2)).toEqual(Buffer.from([0x50, 0x4b]));
  await expect(page.getByRole("row", { name: /Dnevni/ })).toBeVisible();
  const state = await (await request.get("/__fixtures/reports", { headers })).json();
  expect(state.attempts).toBe(1); expect(state.records).toHaveLength(1);
  expect(state.records[0].reportType).toBe("DAILY"); expect(state.records[0].createdBy).toBe(`${process.env.BROWSER_FIXTURE_ROLE}@fixture.test`);
  expect(state.records[0].totalClients).toBe(1); expect(state.records[0].clientsWithBreachedLimit).toBe(1); expect(state.records[0].totalExposure).toBe(125.5); expect(state.records[0].snapshotLength).toBeGreaterThan(20);
  await page.reload(); await interactive(page); await expect(page.getByRole("row", { name: /Dnevni/ })).toBeVisible();
});
test("monthly generation respects actual backend year bounds and persists the selected period", async ({ page, request }) => {
  await open(page); await page.getByLabel("Godina", { exact: true }).fill("2000"); await page.getByLabel("Mjesec", { exact: true }).fill("12");
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Generiši mjesečni", exact: true }).click(); await pending;
  await expect(page.getByRole("row", { name: /Mjesečni/ })).toBeVisible();
  const state = await (await request.get("/__fixtures/reports", { headers })).json();
  expect(state.attempts).toBe(1); expect(state.records).toHaveLength(1); expect(state.records[0].reportType).toBe("MONTHLY"); expect(state.records[0].reportDate).toMatch(/^2000-12-01/);
});
test("failed automatic download does not regenerate a successfully persisted report", async ({ page, request }) => {
  await request.post("/__fixtures/generated-downloads-unavailable", { headers }); await open(page);
  await page.getByRole("button", { name: "Generiši dnevni", exact: true }).click();
  await expect(page.getByText("Izvještaj je snimljen, ali preuzimanje nije uspjelo.", { exact: false })).toBeVisible();
  let state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.attempts).toBe(1); expect(state.records).toHaveLength(1);
  await request.post("/__fixtures/generated-downloads-available", { headers });
  const pending = page.waitForEvent("download"); await page.getByRole("row", { name: /Dnevni/ }).getByRole("button", { name: "Preuzmi", exact: true }).click(); await pending;
  state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.attempts).toBe(1); expect(state.records).toHaveLength(1);
});
test("uncertain post-persist failure is explicit and never causes an automatic duplicate POST", async ({ page, request }) => {
  await request.post("/__fixtures/generation-audit-fails", { headers }); await open(page);
  await page.getByRole("button", { name: "Generiši dnevni", exact: true }).click();
  await expect(page.getByText("Rezultat generisanja nije potvrđen.", { exact: false })).toBeVisible();
  await expect(page.getByText("private-fixture-audit-failure", { exact: false })).toHaveCount(0);
  let state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.attempts).toBe(1); expect(state.records).toHaveLength(1);
  await page.reload(); await interactive(page); await expect(page.getByRole("row", { name: /Dnevni/ })).toBeVisible();
  const pending = page.waitForEvent("download"); await page.getByRole("row", { name: /Dnevni/ }).getByRole("button", { name: "Preuzmi", exact: true }).click(); await pending;
  state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.attempts).toBe(1); expect(state.records).toHaveLength(1);
});
test("all-client and numeric client downloads use real backend files without persisting reports", async ({ page, request }) => {
  await open(page); let pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Svi klijenti", exact: true }).click(); expect((await pending).suggestedFilename()).toMatch(/^svi_klijenti_.*\.xlsx$/);
  await page.getByLabel("Porezni broj ili FBA ID", { exact: true }).fill("ABC");
  await expect(page.getByRole("button", { name: "Preuzmi klijenta", exact: true })).toBeDisabled();
  await page.getByLabel("Porezni broj ili FBA ID", { exact: true }).fill("100000");
  pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Preuzmi klijenta", exact: true }).click(); expect((await pending).suggestedFilename()).toMatch(/^klijent_100000_.*\.xlsx$/);
  const state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.records).toEqual([]); expect(state.attempts).toBe(0);
});
test("archive pagination reaches records beyond the source's first 100", async ({ page, request }) => {
  await request.post("/__fixtures/reports-pages", { headers }); await open(page);
  await expect(page.getByRole("row", { name: /fixture-page-000/ })).toBeVisible(); await expect(page.getByRole("row", { name: /fixture-page-100/ })).toHaveCount(0);
  await page.getByRole("navigation", { name: "Stranice dnevnih izvještaja", exact: true }).getByRole("button", { name: "Sljedeća stranica", exact: true }).click();
  await expect(page.getByRole("row", { name: /fixture-page-100/ })).toBeVisible(); await expect(page.getByRole("row", { name: /fixture-page-000/ })).toHaveCount(0);
});
test("375px report controls and archive have no page overflow", async ({ page, request }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await request.post("/__fixtures/report-existing", { headers }); await open(page);
  await expect(page.getByRole("row", { name: /Dnevni/ })).toBeVisible(); expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("reports-mobile.png"), fullPage: true });
});
test("BS/EN reporting copy and API culture persist", async ({ page, request }) => {
  await open(page); await chooseLanguage(page, "en");

  await expect(page.getByRole("heading", { name: "Regulatory reporting", exact: true })).toBeVisible(); await expect(page.getByRole("button", { name: "Generate daily", exact: true })).toBeVisible();
  await page.reload(); await interactive(page); await expect(page.getByRole("heading", { name: "Regulatory reporting", exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
});

import { expect, test, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
import { readFile } from "node:fs/promises";

const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
async function open(page: Page) { await page.goto("/app/capital"); await interactive(page); }
async function fill(dialog: Locator, regulatory = "123.45", core = "0", supplementary = "67.89") {
  await dialog.getByLabel("Regulatorni kapital", { exact: false }).fill(regulatory);
  await dialog.getByLabel("Osnovni kapital", { exact: false }).fill(core);
  await dialog.getByLabel("Dopunski kapital", { exact: false }).fill(supplementary);
  await dialog.getByLabel("Datum kapitala", { exact: false }).fill("2026-01-01");
}

test("locked period disables capital editing without a mutation and export stays available", async ({ page, request }) => {
  expect((await request.post("/__fixtures/lock", { headers })).status()).toBe(204); await open(page);
  await expect(page.getByRole("button", { name: "Uredi kapital", exact: true })).toBeDisabled();
  await expect(page.getByText("Period je zaključan. Izmjene nisu dozvoljene.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Izvezi Excel", exact: true })).toBeEnabled();
  expect(await (await request.get("/__fixtures/capital", { headers })).json()).toEqual([]);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(0);
});
test("unknown period status fails closed for capital editing and refresh recovers", async ({ page, request }) => {
  await request.post("/__fixtures/period-unavailable", { headers }); await open(page);
  await expect(page.getByRole("button", { name: "Uredi kapital", exact: true })).toBeDisabled();
  await expect(page.getByText(/Stanje perioda trenutno nije dostupno/)).toBeVisible();
  await request.post("/__fixtures/period-available", { headers }); await page.reload(); await interactive(page);
  await expect(page.getByRole("button", { name: "Uredi kapital", exact: true })).toBeEnabled();
});
test("a period locked after opening the capital editor is still rejected by backend", async ({ page, request }) => {
  await open(page); await page.getByRole("button", { name: "Uredi kapital", exact: true }).click();
  const dialog = page.getByRole("dialog"); await fill(dialog);
  await request.post("/__fixtures/lock", { headers }); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Period je zaključan. Izmjene nisu dozvoljene.", { exact: true }).first()).toBeVisible();
  expect(await (await request.get("/__fixtures/capital", { headers })).json()).toEqual([]);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(0);
});
test("actual empty singleton, accessible editor and zero-valued upsert retain one stable record", async ({ page, request }) => {
  await open(page); await expect(page.getByText("Kapital nije postavljen.", { exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("button", { name: "Uredi kapital", exact: true }).click(); const dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Osnovni kapital", { exact: false })).toHaveValue(""); await fill(dialog);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  await expect(page.getByRole("cell", { name: "BAM 0.00", exact: true })).toBeVisible();
  let records = await (await request.get("/__fixtures/capital", { headers })).json(); expect(records).toHaveLength(1);
  const id = records[0].id; expect(records[0].coreCapital).toBe(0); expect(records[0].regulatoryCapital).toBe(123.45);
  await page.getByRole("button", { name: "Uredi kapital", exact: true }).click();
  expect(Number(await dialog.getByLabel("Regulatorni kapital", { exact: false }).inputValue())).toBe(123.45);
  await fill(dialog, "10", "20", "30"); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  records = await (await request.get("/__fixtures/capital", { headers })).json(); expect(records).toHaveLength(1); expect(records[0].id).toBe(id); expect(records[0].coreCapital).toBe(20);
  await page.reload(); await interactive(page); await expect(page.getByRole("cell", { name: "BAM 20.00", exact: true })).toBeVisible();
});
test("required amounts/date and negative values cannot persist", async ({ page, request }) => {
  await open(page); await page.getByRole("button", { name: "Uredi kapital", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Polje je obavezno.", { exact: true }).first()).toBeVisible();
  await fill(dialog, "-1"); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Iznos ne može biti negativan.", { exact: true }).first()).toBeVisible();
  expect(await (await request.get("/__fixtures/capital", { headers })).json()).toEqual([]);
});
test("empty and populated capital export use real backend XLSX", async ({ page, request }, info) => {
  await open(page);
  let pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click();
  let download = await pending; expect(download.suggestedFilename()).toMatch(/^kapital-.*\.xlsx$/);
  await request.post("/__fixtures/limit-reference", { headers }); await page.reload(); await interactive(page);
  await expect(page.getByRole("cell", { name: "BAM 1,234,567.89", exact: true })).toBeVisible();
  pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click(); download = await pending;
  const target = info.outputPath("capital.xlsx"); await download.saveAs(target); expect((await readFile(target)).subarray(0, 2)).toEqual(Buffer.from([0x50, 0x4b]));
});
test("outage stays distinct from empty and retry recovers", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await open(page);
  await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("Kapital nije postavljen.", { exact: true })).toHaveCount(0);
  await expect(page.getByText("private-fixture-detail-not-for-ui", { exact: false })).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByText("Kapital nije postavljen.", { exact: true })).toBeVisible();
});
test("375px capital view/editor retain keyboard focus and no page overflow", async ({ page, request }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await request.post("/__fixtures/limit-reference", { headers }); await open(page);
  const launcher = page.getByRole("button", { name: "Uredi kapital", exact: true }); await launcher.click();
  await expect(page.getByRole("dialog").getByLabel("Datum kapitala", { exact: false })).toHaveValue("2026-01-01");
  await page.keyboard.press("Escape"); await expect(page.getByRole("dialog")).not.toBeVisible(); await expect(launcher).toBeFocused();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("capital-mobile.png"), fullPage: true });
});
test("BS/EN capital wording and API culture persist", async ({ page, request }) => {
  await open(page); await chooseLanguage(page, "en");


  await expect(page.getByRole("heading", { name: "Capital", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Edit capital", exact: true }).click();
  await expect(page.getByRole("dialog").getByLabel("Supplementary capital", { exact: false })).toBeVisible();
  await page.keyboard.press("Escape"); await page.reload(); await interactive(page);
  await expect(page.getByRole("heading", { name: "Capital", exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
});
test("authorized capital upsert is rejected by real locked middleware without writes or audit", async ({ page, request }) => {
  await request.post("/__fixtures/lock", { headers }); await open(page);
  await expect(page.getByRole("button", { name: "Uredi kapital", exact: true })).toBeDisabled();
  const response = await request.post("/__fixtures/authorization-matrix", { headers }); expect(response.status()).toBe(200);
  expect((await response.json()).capital).toBe(423);
  expect(await (await request.get("/__fixtures/capital", { headers })).json()).toEqual([]);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(0);
});

import { expect, test, type Locator, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
import { readFile } from "node:fs/promises";

const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => {
  for (const action of ["reset", "legal-existing", "limit-reference"]) expect((await request.post(`/__fixtures/${action}`, { headers })).status()).toBe(204);
});
async function open(page: Page) { await page.goto("/app/limits"); await interactive(page); }
async function selectEntity(dialog: Locator, name = "Existing Company 00") {
  const combobox = dialog.getByRole("combobox", { name: "Pravno lice", exact: false }); await combobox.fill(name);
  await dialog.getByRole("option", { name, exact: true }).click(); await expect(combobox).toHaveValue(name);
  await expect(dialog.getByRole("heading", { name: "Podaci preuzeti iz backenda", exact: true })).toBeVisible();
}
async function begin(page: Page) {
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click(); const dialog = page.getByRole("dialog");
  await selectEntity(dialog); await dialog.getByLabel("Tip limita", { exact: false }).selectOption("REG"); return dialog;
}
test("empty state and native entity picker use accessible labels", async ({ page }) => {
  await open(page); await expect(page.getByRole("heading", { name: "Nema zapisa", exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  const dialog = await begin(page);
  await expect(dialog.getByText("SERVER-ONLY-789", { exact: true })).toBeVisible();
  await expect(dialog.getByText("BAM 1,234,567.89", { exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("create keeps zero/null and backend-derived capital, edit and confirmed delete persist", async ({ page, request }) => {
  await open(page); let dialog = await begin(page);
  await dialog.getByLabel("Odobreni limit", { exact: false }).fill("0");
  await dialog.getByLabel("Komentar", { exact: false }).fill("Native fixture");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  const row = page.getByRole("row", { name: /Existing Company 00/ }); await expect(row).toBeVisible();
  await page.getByLabel("Pretraži zapise", { exact: true }).fill("  fixture  "); await expect(row).toBeVisible();
  await expect(row.getByText("1.234.567,89", { exact: true })).toBeVisible();
  let records = await (await request.get("/__fixtures/limits", { headers })).json();
  expect(records).toHaveLength(1); expect(records[0].odobreniLimit).toBe(0); expect(records[0].maksimalnaOcekivanaUtilizacija).toBeNull();
  await row.getByRole("button", { name: "Uredi", exact: true }).click(); dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Komentar", { exact: false })).toHaveValue("Native fixture");
  await dialog.getByLabel("Odobreni limit", { exact: false }).fill("123.45");
  await dialog.getByLabel("Maksimalna očekivana utilizacija", { exact: false }).fill("67.89");
  await dialog.getByLabel("Rok očekivane utilizacije", { exact: false }).fill("2026-12-31");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  records = await (await request.get("/__fixtures/limits", { headers })).json(); expect(records[0].odobreniLimit).toBe(123.45); expect(records[0].maksimalnaOcekivanaUtilizacija).toBe(67.89);
  await row.getByRole("button", { name: "Obriši", exact: true }).click();
  await expect(page.getByRole("alertdialog").getByText("Postojeći backend trajno briše", { exact: false })).toBeVisible();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odustani", exact: true }).click(); await expect(row).toBeVisible();
  await row.getByRole("button", { name: "Obriši", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Potvrdi", exact: true }).click(); await expect(row).toHaveCount(0);
  expect(await (await request.get("/__fixtures/limits", { headers })).json()).toEqual([]);
});
test("negative amounts remain field errors without persistence", async ({ page, request }) => {
  await open(page); const dialog = await begin(page);
  await dialog.getByLabel("Odobreni limit", { exact: false }).fill("-1");
  await dialog.getByLabel("Maksimalna očekivana utilizacija", { exact: false }).fill("-2");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Iznos ne može biti negativan.", { exact: true }).first()).toBeVisible();
  expect(await (await request.get("/__fixtures/limits", { headers })).json()).toEqual([]);
});
test("entity search reaches beyond the initial 20 and derives the selected entity only", async ({ page, request }) => {
  await request.post("/__fixtures/legal-pages", { headers }); await open(page);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click(); const dialog = page.getByRole("dialog");
  const combobox = dialog.getByRole("combobox", { name: "Pravno lice", exact: false });
  await combobox.fill("No such company"); await expect(dialog.getByText("Nema rezultata.", { exact: true })).toBeVisible();
  await combobox.fill("Company 21"); await expect(dialog.getByRole("option", { name: "Existing Company 21", exact: true })).toHaveCount(1);
  await combobox.press("ArrowDown"); await expect(combobox).toHaveAttribute("aria-activedescendant", /limit-entity-option-0/); await combobox.press("Escape"); await expect(combobox).toHaveAttribute("aria-expanded", "false");
  await selectEntity(dialog, "Existing Company 21");
  await dialog.getByLabel("Tip limita", { exact: false }).selectOption("REG");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  await expect(page.getByRole("row", { name: /Existing Company 21/ })).toBeVisible();
});
test("filter and metadata search preserve scope and real XLSX download", async ({ page, request }, info) => {
  await request.post("/__fixtures/limit-existing", { headers }); await open(page);
  await page.getByLabel("Pretraži zapise", { exact: true }).fill("fixture");
  const row = page.getByRole("row", { name: /Existing Company 00/ }); await expect(row).toBeVisible();
  const filter = page.getByRole("combobox", { name: "Filtriraj limite po pravnom licu", exact: true }); await filter.fill("Company 00");
  await page.getByRole("option", { name: "Existing Company 00", exact: true }).click();
  await expect(page.getByText("Klijent:", { exact: false })).toBeVisible(); await expect(row).toBeVisible();
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click();
  const download = await pending; expect(download.suggestedFilename()).toMatch(/^limiti-.*\.xlsx$/);
  const target = info.outputPath("limits.xlsx"); await download.saveAs(target); expect((await readFile(target)).subarray(0, 2)).toEqual(Buffer.from([0x50, 0x4b]));
  await page.getByRole("button", { name: "Ukloni filter", exact: true }).click(); await expect(filter).toBeVisible(); await expect(row).toBeVisible();
});
test("actual period lock rejects create without record or audit writes", async ({ page, request }) => {
  await open(page); const dialog = await begin(page);
  await request.post("/__fixtures/lock", { headers });
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Period je zaključan", { exact: false }).first()).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Sačuvaj", exact: true })).toBeDisabled();
  expect(await (await request.get("/__fixtures/limits", { headers })).json()).toEqual([]);
  expect(await (await request.get("/__fixtures/counts", { headers })).json()).toEqual({ persons: 0, audit: 0, mutations: 1 });
});
test("type-codebook outage has no invented fallback and recovers", async ({ page, request }) => {
  await request.post("/__fixtures/codebooks-unavailable", { headers }); await open(page);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  await expect(page.getByRole("alert")).toBeVisible(); await expect(page.getByRole("dialog")).not.toBeVisible();
  await request.post("/__fixtures/available", { headers });
  // Reset only the isolated fixture fault state, not product configuration.
  await request.post("/__fixtures/reset", { headers }); await request.post("/__fixtures/legal-existing", { headers }); await request.post("/__fixtures/limit-reference", { headers });
  await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  const dialog = await begin(page); await expect(dialog.getByLabel("Tip limita", { exact: false }).locator("option")).toHaveCount(2);
});
test("375px form/table and Escape return focus remain usable", async ({ page, request }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await request.post("/__fixtures/limit-existing", { headers }); await open(page);
  const launcher = page.getByRole("button", { name: "Novi zapis", exact: true }); const dialog = await begin(page);
  await page.keyboard.press("Escape"); await expect(dialog).not.toBeVisible(); await expect(launcher).toBeFocused();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("limits-mobile.png"), fullPage: true });
});
test("no active types disables saving instead of inventing a type", async ({ page, request }) => {
  await request.post("/__fixtures/limit-no-types", { headers }); await open(page);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click(); const dialog = page.getByRole("dialog");
  await selectEntity(dialog);
  await expect(dialog.getByText("Nema aktivnih tipova limita", { exact: false })).toBeVisible();
  await expect(dialog.getByLabel("Tip limita", { exact: false }).locator("option")).toHaveCount(1);
  await expect(dialog.getByRole("button", { name: "Sačuvaj", exact: true })).toBeDisabled();
  expect(await (await request.get("/__fixtures/limits", { headers })).json()).toEqual([]);
});
test("BS/EN limit copy persists and reaches the backend", async ({ page, request }) => {
  await open(page); await chooseLanguage(page, "en");


  await expect(page.getByRole("heading", { name: "Limits", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "New record", exact: true }).click();
  await expect(page.getByRole("dialog").getByLabel("Approved limit", { exact: false })).toBeVisible();
  await page.keyboard.press("Escape"); await page.reload(); await interactive(page);
  await expect(page.getByRole("heading", { name: "Limits", exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
});

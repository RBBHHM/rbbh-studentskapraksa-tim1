import { expect, test, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";
import { readFile } from "node:fs/promises";
import path from "node:path";

const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
async function open(page: Page) { await page.goto("/app/legal-persons"); await interactive(page); }
async function fill(dialog: Locator, name: string, fba = "123456") {
  await dialog.getByLabel("Naziv", { exact: false }).fill(name);
  await dialog.getByRole("radiogroup", { name: "Rezidentnost", exact: false }).getByRole("radio", { name: "Nerezident", exact: true }).check();
  await dialog.getByLabel("FBA ID", { exact: false }).fill(fba);
  await dialog.getByLabel("Matični broj", { exact: false }).fill("12345");
  await dialog.getByLabel("Osnov povezanosti", { exact: false }).selectOption("ZOB-2-V-5");
  await dialog.getByRole("radiogroup", { name: "Povezan s bankom", exact: false }).getByRole("radio", { name: "Da", exact: true }).check();
  await dialog.getByLabel("Datum od", { exact: false }).fill("2026-01-01");
}
async function create(page: Page, name: string, fba = "123456") {
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog"); await fill(dialog, name, fba);
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).not.toBeVisible(); await expect(page.getByRole("row", { name: new RegExp(name) })).toBeVisible();
}

test("empty legal route and real native form have accessible labels", async ({ page }) => {
  await open(page);
  await expect(page.getByRole("heading", { name: "Pravna lica", exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Nema zapisa", exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog"); await fill(dialog, "Accessible Company");
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("nonresident save preserves explicit false and optional end date", async ({ page, request }) => {
  await open(page); await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog"); await fill(dialog, "Native Company");
  await expect(dialog.getByLabel("Porezni broj", { exact: false })).toBeDisabled();
  await dialog.getByRole("radiogroup", { name: "Povezan s bankom", exact: false }).getByRole("radio", { name: "Ne", exact: true }).check();
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  await expect(page.getByRole("row", { name: /Native Company/ })).toBeVisible();
  const records = await (await request.get("/__fixtures/legal-record", { headers })).json();
  expect(records).toHaveLength(1); expect(records[0].isActive).toBe(true);
  expect(records[0].connectedWithBank).toBe(false); expect(records[0].dateTo).toBeNull();
});
test("resident tax checksum, registration and date-order validation precede real save", async ({ page }) => {
  await open(page); await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog"); await fill(dialog, "Resident Company");
  await dialog.getByRole("radiogroup", { name: "Rezidentnost", exact: false }).getByRole("radio", { name: "Rezident", exact: true }).check();
  await expect(dialog.getByLabel("FBA ID", { exact: false })).toBeDisabled();
  await dialog.getByLabel("Porezni broj", { exact: false }).fill("1234567890123");
  await dialog.getByLabel("Matični broj", { exact: false }).fill("ABC");
  await dialog.getByLabel("Datum do", { exact: false }).fill("2025-12-31");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Provjerite porezni broj:", { exact: false }).first()).toBeVisible();
  await expect(dialog.getByText("Matični broj smije sadržavati", { exact: false }).first()).toBeVisible();
  await expect(dialog.getByText("Datum do ne može biti prije datuma od.", { exact: true }).first()).toBeVisible();
  await dialog.getByLabel("Porezni broj", { exact: false }).fill("1234567890125");
  await dialog.getByLabel("Matični broj", { exact: false }).fill("98765");
  await dialog.getByLabel("Datum do", { exact: false }).fill("2026-12-31");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
});
test("duplicate FBA receives safe validation and does not add a record", async ({ page, request }) => {
  await open(page); await create(page, "Original Company");
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog"); await fill(dialog, "Duplicate Company");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Backend je odbio podatke. Provjerite unesena polja.", { exact: true }).first()).toBeVisible();
  expect(await (await request.get("/__fixtures/legal-record", { headers })).json()).toHaveLength(1);
  await expect(dialog).toBeVisible();
});
test("edit retains hidden Matbroj, verifies and soft deactivates through real backend", async ({ page, request }) => {
  expect((await request.post("/__fixtures/legal-existing", { headers })).status()).toBe(204);
  await open(page); let row = page.getByRole("row", { name: /Existing Company 00/ });
  await row.getByRole("button", { name: "Uredi", exact: true }).click();
  const dialog = page.getByRole("dialog"); await expect(dialog.getByLabel("Matični broj", { exact: false })).toHaveValue("12345");
  await dialog.getByLabel("Naziv", { exact: false }).fill("Updated Company");
  await dialog.getByLabel("Matični broj", { exact: false }).fill("98765");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  let record = (await (await request.get("/__fixtures/legal-record", { headers })).json())[0];
  expect(record.matbroj).toBe("SERVER-ONLY-789"); expect(record.maticniBroj).toBe("98765");
  row = page.getByRole("row", { name: /Updated Company/ });
  await row.getByRole("button", { name: "Verificiraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(page.getByRole("alertdialog")).not.toBeVisible();
  await expect(row.getByRole("button", { name: "Verificiraj", exact: true })).toHaveCount(0);
  record = (await (await request.get("/__fixtures/legal-record", { headers })).json())[0];
  expect(record.status).toBe("Verified"); expect(record.verifiedBy).toBeTruthy();
  await row.getByRole("button", { name: "Uredi", exact: true }).click();
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).not.toBeVisible();
  record = (await (await request.get("/__fixtures/legal-record", { headers })).json())[0];
  expect(record.status).toBe("Verified"); expect(record.matbroj).toBe("SERVER-ONLY-789");
  await row.getByRole("button", { name: "Deaktiviraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(row).toHaveCount(0);
  record = (await (await request.get("/__fixtures/legal-record", { headers })).json())[0]; expect(record.isActive).toBe(false);
});
test("paged data remains reachable and metadata search stays scoped to displayed page", async ({ page, request }) => {
  await request.post("/__fixtures/legal-pages", { headers }); await open(page);
  await expect(page.getByRole("row", { name: /Existing Company 21/ })).toBeVisible();
  await page.getByLabel("Pretraga podataka na prikazanoj stranici", { exact: true }).fill("fixture-author");
  await expect(page.getByRole("row", { name: /Existing Company 21/ })).toBeVisible();
  await page.getByLabel("Pretraga podataka na prikazanoj stranici", { exact: true }).fill("  fixture-author  ");
  await expect(page.getByRole("row", { name: /Existing Company 21/ })).toBeVisible();
  await page.getByRole("button", { name: "Sljedeća stranica", exact: true }).click();
  await expect(page.getByRole("row", { name: /Existing Company 00/ })).toBeVisible();
  await expect(page.getByRole("row", { name: /Existing Company 21/ })).toHaveCount(0);
  await page.getByRole("button", { name: "Prethodna stranica", exact: true }).click();
  await expect(page.getByRole("row", { name: /Existing Company 21/ })).toBeVisible();
});
test("real backend XLSX download and import retain counts and safe schema guidance", async ({ page }, info) => {
  await open(page); await create(page, "Export Company");
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click();
  const download = await pending; expect(download.suggestedFilename()).toMatch(/^pravna-lica-.*\.xlsx$/);
  const target = info.outputPath("legal.xlsx"); await download.saveAs(target);
  expect((await readFile(target)).subarray(0, 2)).toEqual(Buffer.from([0x50, 0x4b]));
  await page.getByRole("button", { name: "Uvezi Excel", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Excel datoteka").setInputFiles(path.resolve("../../artifacts/migration-browser-fixtures/invalid-physical.xlsx"));
  await dialog.getByRole("button", { name: "Uvezi Excel", exact: true }).click();
  await expect(dialog.getByText("Predložak mora imati 10 kolona", { exact: false })).toBeVisible();
  await dialog.getByLabel("Excel datoteka").setInputFiles(path.resolve("../../artifacts/migration-browser-fixtures/valid-legal.xlsx"));
  await dialog.getByRole("button", { name: "Uvezi Excel", exact: true }).click();
  await expect(dialog.getByText("Uvezeno: 1", { exact: false })).toBeVisible();
  await dialog.getByRole("button", { name: "Zatvori", exact: true }).click();
  await expect(page.getByRole("row", { name: /Imported Company/ })).toBeVisible();
});
test("locked period disables legal verification and preserves draft state and audit", async ({ page, request }) => {
  await request.post("/__fixtures/legal-existing", { headers }); await request.post("/__fixtures/lock", { headers }); await open(page);
  await expect(page.getByRole("button", { name: "Verificiraj", exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Uredi", exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Deaktiviraj", exact: true })).toBeDisabled();
  await expect(page.getByText("Period je zaključan. Izmjene nisu dozvoljene.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/legal-record", { headers })).json())[0].status).toBe("Draft");
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(0);
});
test("unavailable period disables legal verification until an explicit refresh", async ({ page, request }) => {
  await request.post("/__fixtures/legal-existing", { headers }); await request.post("/__fixtures/period-unavailable", { headers }); await open(page);
  await expect(page.getByRole("button", { name: "Verificiraj", exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Uredi", exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Deaktiviraj", exact: true })).toBeDisabled();
  await expect(page.getByText(/Stanje perioda trenutno nije dostupno/)).toBeVisible();
  await request.post("/__fixtures/period-available", { headers }); await page.getByRole("button", { name: "Osvježi", exact: true }).click();
  await expect(page.getByRole("button", { name: "Verificiraj", exact: true })).toBeEnabled();
});
test("locked real period rejects persistence with zero records and audit writes", async ({ page, request }) => {
  await open(page);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click(); const dialog = page.getByRole("dialog"); await fill(dialog, "Blocked Company");
  await request.post("/__fixtures/lock", { headers });
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Period je zaključan", { exact: false }).first()).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Sačuvaj", exact: true })).toBeDisabled();
  expect(await (await request.get("/__fixtures/legal-record", { headers })).json()).toHaveLength(0);
  expect(await (await request.get("/__fixtures/counts", { headers })).json()).toEqual({ persons: 0, audit: 0, mutations: 1 });
});
test("outage remains redacted and retry recovers without synthetic data", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await open(page);
  await expect(page.getByRole("alert")).toBeVisible(); await expect(page.getByText("private-fixture-detail-not-for-ui")).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByRole("button", { name: "Novi zapis", exact: true })).toBeVisible();
});
test("codebook fallback is visible and Escape restores launcher focus", async ({ page, request }) => {
  await request.post("/__fixtures/codebooks-unavailable", { headers }); await open(page);
  const launcher = page.getByRole("button", { name: "Novi zapis", exact: true }); await launcher.click();
  const dialog = page.getByRole("dialog"); await expect(dialog.getByText("šifrarn", { exact: false }).first()).toBeVisible();
  await expect(dialog.getByLabel("Osnov povezanosti", { exact: false }).locator("option")).toHaveCount(8);
  await page.keyboard.press("Escape"); await expect(dialog).not.toBeVisible(); await expect(launcher).toBeFocused();
});
test("375px legal table and form remain usable without page overflow", async ({ page }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await open(page); await create(page, "Mobile Company");
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("legal-mobile.png"), fullPage: true });
});
test("legal BS/EN copy persists with backend culture propagation", async ({ page, request }) => {
  await open(page); await page.getByLabel("Jezik", { exact: true }).selectOption("en");
  await expect(page.locator("form.app-language")).toHaveAttribute("action", /\/en\?/);
  await page.getByRole("button", { name: "Primijeni", exact: true }).click(); await interactive(page);
  await expect(page).toHaveURL(/\/app\/legal-persons$/);
  await expect(page.getByRole("heading", { name: "Legal entities", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "New record", exact: true }).click();
  await expect(page.getByRole("dialog").getByLabel("Tax number", { exact: false })).toBeVisible();
  await page.keyboard.press("Escape"); await page.reload(); await interactive(page);
  await expect(page.getByRole("heading", { name: "Legal entities", exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
});

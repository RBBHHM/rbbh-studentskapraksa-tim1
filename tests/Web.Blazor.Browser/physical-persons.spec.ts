import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, openPhysical } from "./readiness";
import { readFile } from "node:fs/promises";
import path from "node:path";

test.beforeEach(async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers: { "X-Fixture-Control": "true" } })).status()).toBe(204);
});

test("actual backend empty state, labels and accessibility", async ({ page }) => {
  await openPhysical(page);
  await expect(page.getByRole("heading", { name: "Fizička lica", exact: true })).toBeVisible();
  await expect(page.getByText("Nema zapisa za prikaz", { exact: false })).toBeVisible();
  await expect(page.getByRole("button", { name: "Novi zapis", exact: true })).toBeVisible();
  const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze();
  expect(results.violations).toEqual([]);
});

test("native two-step form keeps explicit choices and saves through real backend", async ({ page }) => {
  page.on("console", message => { if (message.type() === "error") console.log(`Fixture browser error: ${message.text()}`); });
  await openPhysical(page);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel(/^Ime/).fill("Native");
  await dialog.getByLabel(/^Prezime/).fill("Fixture");
  await dialog.getByLabel("Rezidentnost", { exact: false }).selectOption("2");
  await dialog.getByLabel("Broj pasoša", { exact: false }).fill("NAT12345");
  await dialog.getByLabel("FBA ID", { exact: false }).fill("123456");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await dialog.getByLabel("Osnov povezanosti", { exact: false }).selectOption("ZOB-2-V-5");
  await expect(dialog.getByLabel("Opis povezanosti", { exact: false })).toHaveValue("Fixture relationship description");
  await dialog.getByLabel("Osnov posebnog odnosa", { exact: false }).selectOption("UPRAVA");
  for (const label of ["Identifikovani zaposlenik", "Povezan s bankom"]) await dialog.getByRole("radiogroup", { name: label, exact: false }).getByRole("radio", { name: "Da", exact: true }).check();
  for (const label of ["U posebnom odnosu s bankom", "Poseban ugovor", "Malus & Clawback", "Nema članova porodice"]) await dialog.getByRole("radiogroup", { name: label, exact: false }).getByRole("radio", { name: "Ne", exact: true }).check();
  await dialog.getByLabel("Datum od", { exact: false }).fill("2026-01-01");
  await dialog.getByLabel("Datum do", { exact: false }).fill("2026-12-31");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await expect(page.getByRole("row", { name: /Native Fixture/ })).toBeVisible();
  await page.reload();
  await interactive(page);
  await expect(page.getByRole("row", { name: /Native Fixture/ })).toBeVisible();
});

test("375px route retains usable form and data table without page overflow", async ({ page }, info) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await openPhysical(page);
  await expect(page.getByRole("heading", { name: "Fizička lica", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Novi zapis", exact: true })).toBeVisible();
  await createNonresident(page, "Mobile", "MOB12345", "666777");
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("physical-mobile.png"), fullPage: true });
});

test("resident identity validation blocks invalid JMBG and saves a valid resident", async ({ page }) => {
  await openPhysical(page);
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel(/^Ime/).fill("Resident");
  await dialog.getByLabel(/^Prezime/).fill("Fixture");
  await dialog.getByLabel("Rezidentnost", { exact: false }).selectOption("1");
  await expect(dialog.getByLabel("Broj pasoša", { exact: false })).toBeDisabled();
  await dialog.getByLabel("JMBG", { exact: false }).fill("0101990500000");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await expect(dialog.getByText("Provjerite JMBG:", { exact: false }).first()).toBeVisible();
  await expect(dialog.getByLabel("JMBG", { exact: false })).toBeVisible();
  await dialog.getByLabel("JMBG", { exact: false }).fill("0101990500003");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await finishBusiness(dialog);
  await expect(page.getByRole("row", { name: /Resident Fixture/ })).toBeVisible();
});

test("duplicates block progression, edit excludes self, verification and deactivation refresh", async ({ page }) => {
  await openPhysical(page);
  await createNonresident(page, "Lifecycle", "LIF12345", "456789");
  let row = page.getByRole("row", { name: /Lifecycle Fixture/ });
  await row.getByRole("button", { name: "Uredi", exact: true }).click();
  let dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel(/^Ime/)).toHaveValue("Lifecycle");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await expect(dialog.getByRole("radiogroup", { name: "Malus & Clawback", exact: false }).getByRole("radio", { name: "Ne", exact: true })).toBeChecked();
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await row.getByRole("button", { name: "Verificiraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(row.getByText("Verificirano", { exact: true })).toBeVisible();
  await expect(row.getByRole("button", { name: "Verificiraj", exact: true })).toHaveCount(0);
  await beginNonresident(page, "Duplicate", "LIF12345", "765432");
  dialog = page.getByRole("dialog");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await expect(dialog.getByText("Zapis s ovom identifikacijom već postoji.").first()).toBeVisible();
  await expect(dialog.getByLabel("Broj pasoša", { exact: false })).toBeVisible();
  await dialog.getByRole("button", { name: "Odustani", exact: true }).click();
  await row.getByRole("button", { name: "Deaktiviraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(row).toHaveCount(0);
  await page.reload(); await interactive(page);
  await expect(page.getByRole("row", { name: /Lifecycle Fixture/ })).toHaveCount(0);
});

test("unified family form locks required flags and backend relationship tree survives reload", async ({ page }) => {
  await openPhysical(page);
  await createNonresident(page, "Parent", "PAR12345", "111222");
  const dialog = await beginNonresident(page, "Family", "FAM12345", "222333");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await dialog.getByLabel("Osnov povezanosti", { exact: false }).selectOption("ZOB-2-V-5");
  await dialog.getByLabel("Osnov posebnog odnosa", { exact: false }).selectOption("UZA_PORODICA");
  await expect(dialog.getByRole("radiogroup", { name: "Identifikovani zaposlenik", exact: false }).getByRole("radio", { name: "Ne", exact: true })).toBeChecked();
  await expect(dialog.getByRole("radiogroup", { name: "Identifikovani zaposlenik", exact: false }).getByRole("radio", { name: "Da", exact: true })).toBeDisabled();
  await dialog.getByLabel("Povezano s fizičkim licem", { exact: false }).selectOption({ label: "Parent Fixture" });
  await dialog.getByLabel("Datum od", { exact: false }).fill("2026-01-01");
  await dialog.getByLabel("Datum do", { exact: false }).fill("2026-12-31");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await page.getByLabel("Odaberite fizičko lice za prikaz stabla.").selectOption({ label: "Parent Fixture" });
  await expect(page.locator(".registry-tree strong").filter({ hasText: "Family Fixture" })).toBeVisible();
  await page.reload(); await interactive(page);
  await page.getByLabel("Odaberite fizičko lice za prikaz stabla.").selectOption({ label: "Parent Fixture" });
  await expect(page.locator(".registry-tree strong").filter({ hasText: "Family Fixture" })).toBeVisible();
  await page.getByRole("row", { name: /Family Fixture/ }).getByRole("button", { name: "Uredi", exact: true }).click();
  await dialog.getByLabel(/^Ime/).fill("FamilyUpdated");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await expect(page.locator(".registry-tree strong").filter({ hasText: "FamilyUpdated Fixture" })).toBeVisible();
  await page.getByRole("row", { name: /FamilyUpdated Fixture/ }).getByRole("button", { name: "Deaktiviraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(page.getByRole("row", { name: /FamilyUpdated Fixture/ })).toHaveCount(0);
  await expect(page.locator(".registry-tree strong").filter({ hasText: "FamilyUpdated Fixture" })).toHaveCount(0);
});

test("backend XLSX export delivers the attachment bytes and filename", async ({ page }, info) => {
  await openPhysical(page);
  await createNonresident(page, "Export", "EXP12345", "333444");
  const pending = page.waitForEvent("download");
  await page.getByRole("button", { name: "Preuzmi Excel", exact: true }).click();
  const download = await pending;
  expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);
  const target = info.outputPath("physical.xlsx");
  await download.saveAs(target);
  expect((await readFile(target)).subarray(0, 2)).toEqual(Buffer.from([0x50, 0x4b]));
});

test("valid backend workbook import refreshes list; invalid schema does not create records", async ({ page }) => {
  await openPhysical(page);
  await page.getByRole("button", { name: "Uvoz", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Excel datoteka").setInputFiles(path.resolve("../../artifacts/migration-browser-fixtures/invalid-physical.xlsx"));
  await dialog.getByRole("button", { name: "Uvoz", exact: true }).click();
  await expect(dialog.getByText("Uvezeno: 0.", { exact: false })).toBeVisible();
  await expect(dialog.getByText("Predložak mora imati 15 kolona; pronađeno: 1.", { exact: true })).toBeVisible();
  await dialog.getByLabel("Excel datoteka").setInputFiles(path.resolve("../../artifacts/migration-browser-fixtures/valid-physical.xlsx"));
  await dialog.getByRole("button", { name: "Uvoz", exact: true }).click();
  await expect(dialog.getByText("Uvezeno: 1. Neuspješno: 0.")).toBeVisible();
  await dialog.getByRole("button", { name: "Zatvori", exact: true }).click();
  await expect(page.getByRole("row", { name: /Import Fixture/ })).toBeVisible();
});

test("modal keyboard focus returns to trigger and business step is accessible", async ({ page }) => {
  await openPhysical(page);
  let dialog = await beginNonresident(page, "Focus", "FOC12345", "444555");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await expect(dialog.locator("legend").first()).toBeFocused();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.keyboard.press("Escape");
  await expect(dialog).not.toBeVisible();
  await expect(page.getByRole("button", { name: "Novi zapis", exact: true })).toBeFocused();
});

test("actual period middleware rejects persistence with423 and safe inline recovery", async ({ page, request }) => {
  expect((await request.post("/__fixtures/lock", { headers: { "X-Fixture-Control": "true" } })).status()).toBe(204);
  await openPhysical(page);
  const dialog = await beginNonresident(page, "Locked", "LOC12345", "777888");
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await fillBusiness(dialog);
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Period je zaključan. Izmjene nisu dozvoljene.").first()).toBeVisible();
  await expect(dialog).toBeVisible();
  const counts = await (await request.get("/__fixtures/counts", { headers: { "X-Fixture-Control": "true" } })).json();
  expect(counts).toEqual({ persons: 0, audit: 0, mutations: 1 });
});

test("backend unavailable fails safely and recovers, without internal response text", async ({ page, request }) => {
  expect((await request.post("/__fixtures/unavailable", { headers: { "X-Fixture-Control": "true" } })).status()).toBe(204);
  await openPhysical(page);
  await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("private-fixture-detail-not-for-ui", { exact: false })).toHaveCount(0);
  expect((await request.post("/__fixtures/available", { headers: { "X-Fixture-Control": "true" } })).status()).toBe(204);
  await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByRole("button", { name: "Novi zapis", exact: true })).toBeVisible();
});

test("existing business codebook fallback remains available during reference outage", async ({ page, request }) => {
  expect((await request.post("/__fixtures/codebooks-unavailable", { headers: { "X-Fixture-Control": "true" } })).status()).toBe(204);
  await openPhysical(page);
  const dialog = await beginNonresident(page, "Fallback", "FAL12345", "888999");
  await expect(dialog.getByText("Privremeno se koriste postojeće rezervne vrijednosti šifrarnika.")).toBeVisible();
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await dialog.getByLabel("Osnov povezanosti", { exact: false }).selectOption("ZOB-2-V-5");
  await expect(dialog.getByLabel("Opis povezanosti", { exact: false })).toHaveValue(/Član nadzornog odbora/);
});

test("BS/EN cookie selection persists and reaches the actual backend", async ({ page, request }) => {
  await openPhysical(page);
  await page.getByLabel("Jezik", { exact: true }).selectOption("en");
  await expect(page.locator("form.app-language")).toHaveAttribute("action", /\/en\?/);
  await page.getByRole("button", { name: "Primijeni", exact: true }).click();
  await interactive(page);
  await expect(page.getByRole("heading", { name: "Physical persons", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "New record", exact: true })).toBeVisible();
  await page.reload(); await interactive(page);
  await expect(page.getByRole("heading", { name: "Physical persons", exact: true })).toBeVisible();
  const culture = await (await request.get("/__fixtures/culture", { headers: { "X-Fixture-Control": "true" } })).json();
  expect(culture.language).toBe("en");
  await expect(page.getByRole("option", { name: "Deutsch", exact: true })).toHaveCount(0);
});

test("search covers identity and existing backend audit values, with no-result recovery", async ({ page }) => {
  await openPhysical(page);
  await createNonresident(page, "Search", "SEA12345", "909090");
  const search = page.getByLabel("Pretraži zapise", { exact: true });
  await search.fill("does-not-exist");
  await expect(page.getByRole("row", { name: /Search Fixture/ })).toHaveCount(0);
  await expect(page.getByText("Nema zapisa za prikaz", { exact: false })).toBeVisible();
  await search.fill("SEA12345");
  await expect(page.getByRole("row", { name: /Search Fixture/ })).toBeVisible();
  await search.fill("FL@fixture.test");
  await expect(page.getByRole("row", { name: /Search Fixture/ })).toBeVisible();
  await search.fill("  FL@fixture.test  ");
  await expect(page.getByRole("row", { name: /Search Fixture/ })).toBeVisible();
  await search.fill("");
  await expect(page.getByRole("row", { name: /Search Fixture/ })).toBeVisible();
});

async function beginNonresident(page: import("@playwright/test").Page, name: string, passport: string, fba: string) {
  await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel(/^Ime/).fill(name); await dialog.getByLabel(/^Prezime/).fill("Fixture");
  await dialog.getByLabel("Rezidentnost", { exact: false }).selectOption("2");
  await dialog.getByLabel("Broj pasoša", { exact: false }).fill(passport);
  await dialog.getByLabel("FBA ID", { exact: false }).fill(fba);
  return dialog;
}
async function finishBusiness(dialog: import("@playwright/test").Locator) {
  await fillBusiness(dialog);
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).not.toBeVisible();
}
async function fillBusiness(dialog: import("@playwright/test").Locator) {
  await dialog.getByLabel("Osnov povezanosti", { exact: false }).selectOption("ZOB-2-V-5");
  await dialog.getByLabel("Osnov posebnog odnosa", { exact: false }).selectOption("UPRAVA");
  for (const label of ["Identifikovani zaposlenik", "Povezan s bankom"]) await dialog.getByRole("radiogroup", { name: label, exact: false }).getByRole("radio", { name: "Da", exact: true }).check();
  for (const label of ["U posebnom odnosu s bankom", "Poseban ugovor", "Malus & Clawback", "Nema članova porodice"]) await dialog.getByRole("radiogroup", { name: label, exact: false }).getByRole("radio", { name: "Ne", exact: true }).check();
  await dialog.getByLabel("Datum od", { exact: false }).fill("2026-01-01");
  await dialog.getByLabel("Datum do", { exact: false }).fill("2026-12-31");
}
async function createNonresident(page: import("@playwright/test").Page, name: string, passport: string, fba: string) {
  const dialog = await beginNonresident(page, name, passport, fba);
  await dialog.getByRole("button", { name: "Nastavi", exact: true }).click();
  await finishBusiness(dialog);
  await expect(page.getByRole("row", { name: new RegExp(`${name} Fixture`) })).toBeVisible();
}

import { readFile } from "node:fs/promises";
import { expect, test, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
async function openCategory(page: Page, category: string) { await page.getByRole("button", { name: category, exact: true }).click(); await expect(page.locator("caption, .registry-category").first()).toBeVisible(); await expect(page.locator("#code-file")).toBeEnabled(); }
async function openFirst(page: Page) { await page.locator(".codes-card").first().click(); await expect(page.getByRole("button", { name: "Svi šifrarnici", exact: true })).toBeVisible(); }
async function visit(page: Page, category?: string) { await page.goto("/app/admin/code-lists"); await interactive(page); if (category) await openCategory(page, category); }
async function settled(dialog: Locator) {
  await expect(dialog).toBeVisible(); await dialog.evaluate(async element => { await Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished.catch(() => {}))); });
  await expect(dialog).toHaveCSS("opacity", "1");
}
async function fillValue(page: Page, code: string, name: string) {
  await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill(name); await dialog.getByRole("textbox", { name: /^Kod/ }).fill(code); return dialog;
}
async function configureColumn(dialog: Locator, slot: string, name: string, position: string) {
  await dialog.getByLabel("Kolona i tip podataka").selectOption(slot);
  await dialog.getByRole("checkbox", { name: "Uključi kolonu" }).press("Space");
  await dialog.getByLabel("Naziv kolone").fill(name);
  await dialog.getByLabel("Redoslijed (1–20)").fill(position);
}
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });

test("real categories/history and native definition/value forms remain accessible", async ({ page }) => {
  await visit(page); await expect(page.getByRole("heading", { name: "Šifrarnici", exact: true })).toBeVisible();
  await expect(page.getByLabel("Pretraga šifrarnika", { exact: false })).toBeVisible(); await openFirst(page);
  await expect(page.getByText(/Administrativni pregled uključuje neaktivne/)).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("button", { name: "Novi šifrarnik", exact: true }).click(); await settled(page.getByRole("dialog"));
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("dialog").getByRole("button", { name: "Odustani", exact: true }).click();
  await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click(); await settled(page.getByRole("dialog"));
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("custom definition controls create, edit, CSV headers and soft-delete", async ({ page, request }) => {
  await visit(page);
  await page.getByRole("button", { name: "Novi šifrarnik", exact: true }).click();
  let dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: /^Naziv definicije/ }).fill("NativeCodes");
  await dialog.getByRole("textbox", { name: "Poslovna svrha", exact: true }).fill("Native fixture purpose");
  await configureColumn(dialog, "17", "Poruka", "1");
  await configureColumn(dialog, "3", "Količina", "2");
  await dialog.getByRole("button", { name: "Kreiraj", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "NATIVE_CODES" })).toBeVisible();

  const pending = page.waitForEvent("download");
  await page.getByRole("button", { name: "Preuzmi CSV predložak", exact: true }).click();
  const download = await pending;
  const template = await readFile((await download.path())!, "utf8");
  expect(template.trimStart().split("\r\n")[0]).toBe('"Poruka","Količina"');

  await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click();
  dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Kod", { exact: true })).toHaveCount(0);
  await dialog.getByLabel("Poruka").fill("Prva poruka");
  await dialog.getByLabel("Količina").fill("42");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  const card = page.locator(".custom-codebook-values .rbi-card");
  await expect(card).toContainText("Prva poruka");
  await expect(card).toContainText("42");

  await card.getByRole("button", { name: "Uredi vrijednost" }).click();
  dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Količina")).toHaveValue("42");
  await dialog.getByLabel("Poruka").fill("Druga poruka");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(card).toContainText("Druga poruka");

  await page.getByRole("button", { name: "Uredi kolone" }).click();
  dialog = page.getByRole("dialog");
  await dialog.getByLabel("Naziv kolone").fill("Napomena");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(card).toContainText("Napomena");
  await expect(card).toContainText("Druga poruka");

  await card.getByRole("button", { name: "Obriši vrijednost" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click();
  await expect(card).toContainText("Neaktivan");
  const state = await (await request.get("/__fixtures/codes-state", { headers })).json();
  expect(state.values.find((item: any) => item.kategorija === "NATIVE_CODES")).toMatchObject({ aktivan: false });
});

test("changed reference value immediately reaches the actual business form dropdown", async ({ page }) => {
  await visit(page, "OSNOV_POVEZANOSTI"); await page.getByRole("button", { name: "Uredi vrijednost", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill("Updated relationship basis"); await dialog.getByRole("textbox", { name: "Opis", exact: true }).fill("Updated reference description");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).toHaveCount(0);
  await page.goto("/app/legal-persons"); await interactive(page); await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const business = page.getByRole("dialog"); const basis = business.getByLabel("Osnov povezanosti", { exact: false });
  await expect(basis.locator("option[value='ZOB-2-V-5']")).toHaveText("Updated relationship basis"); await basis.selectOption("ZOB-2-V-5");
  await expect(business.getByLabel("Opis povezanosti", { exact: false })).toHaveValue("Updated reference description");
});
test("validation blocks requests and duplicate code remains an authoritative backend rejection", async ({ page, request }) => {
  await visit(page, "OSNOV_POVEZANOSTI"); await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click(); let dialog = page.getByRole("dialog");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog.getByText("Naziv je obavezan").first()).toBeVisible();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill("Duplicate attempt"); await dialog.getByRole("textbox", { name: /^Kod/ }).fill("ZOB-2-V-5");
  await page.keyboard.press("Tab");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Backend je odbio podatke. Provjerite unesena polja.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/codes-state", { headers })).json()).audit).toEqual([]);
});
test("referenced value and entire category deletion are rejected with no data/audit change", async ({ page, request }) => {
  await request.post("/__fixtures/legal-existing", { headers }); await visit(page, "OSNOV_POVEZANOSTI");
  const before = await (await request.get("/__fixtures/codes-state", { headers })).json();
  await page.getByRole("button", { name: "Obriši vrijednost", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click();
  await expect(page.getByText(/Brisanje je odbijeno:/)).toBeVisible(); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await page.getByRole("button", { name: "Obriši šifrarnik", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click(); await expect(page.getByText(/Brisanje je odbijeno:/)).toBeVisible();
  expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(before);
});
test("category soft-delete keeps inactive administrative history instead of pretending physical removal", async ({ page, request }) => {
  await request.post("/__fixtures/codes-reference", { headers }); await visit(page, "MigrationCodes");
  await page.getByRole("button", { name: "Obriši šifrarnik", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click(); await expect(page.getByText("Šifrarnik i njegove vrijednosti su obrisani.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Svi šifrarnici", exact: true })).toBeVisible(); expect(await page.getByRole("cell", { name: "Neaktivan", exact: true }).count()).toBe(2);
  const state = await (await request.get("/__fixtures/codes-state", { headers })).json(); expect(state.definitions).toEqual([{ name: "MigrationCodes", description: "Fixture purpose", isActive: false }]);
  expect(state.values.filter((item: any) => item.kategorija === "MigrationCodes").every((item: any) => !item.aktivan)).toBe(true); expect(state.audit.map((item: any) => item.action)).toEqual(["CODELIST_DELETE"]);
});
test("CSV template, preview and confirmation use the selected codebook", async ({ page, request }) => {
  await request.post("/__fixtures/codes-reference", { headers });
  await visit(page, "MigrationCodes");
  const pending = page.waitForEvent("download");
  await page.getByRole("button", { name: "Preuzmi CSV predložak", exact: true }).click();
  const download = await pending;
  expect(download.suggestedFilename()).toBe("codebook-template.csv");
  const template = await readFile((await download.path())!, "utf8");
  expect(template).toContain('"VALUE_KEY","NAME","DESCRIPTION","IS_ACTIVE"');
  expect(template).toContain('"EXAMPLE_CODE"');

  const state = async () => (await request.get("/__fixtures/codes-state", { headers })).json();
  const before = await state();
  await page.locator("#code-file").setInputFiles({
    name: "valid.csv", mimeType: "text/csv",
    buffer: Buffer.from(template + 'NOVI_KOD,"Novi unos, s opisom","Tekst",true\r\n'),
  });
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByText("Primjer iz predloška je preskočen.", { exact: true })).toBeVisible();
  await expect(dialog.getByText(/Spremno za uvoz: 1/)).toBeVisible();
  expect(await state()).toEqual(before);
  await dialog.getByRole("button", { name: "Uvezi vrijednosti", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole("row").filter({ hasText: "NOVI_KOD" })).toContainText("Novi unos, s opisom");
  expect((await state()).audit.map((item: any) => item.action)).toEqual(["IMPORT"]);
});
test("invalid CSV shows a header or cell error and cannot write values", async ({ page, request }) => {
  await request.post("/__fixtures/codes-reference", { headers });
  await visit(page, "MigrationCodes");
  const input = page.locator("#code-file");
  const state = async () => (await request.get("/__fixtures/codes-state", { headers })).json();
  const before = await state();
  await input.setInputFiles({ name: "wrong.txt", mimeType: "text/plain", buffer: Buffer.from("wrong") });
  await expect(page.getByText("Odaberite nepraznu .csv datoteku do 10 MB.", { exact: true })).toBeVisible();
  await input.setInputFiles({ name: "header.csv", mimeType: "text/csv", buffer: Buffer.from("BAD\nfoo") });
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByText("Zaglavlje sadrži neočekivanu kolonu „BAD”.", { exact: true })).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Uvezi vrijednosti", exact: true })).toBeDisabled();
  await dialog.getByRole("button", { name: "Odustani", exact: true }).click();
  await input.setInputFiles({ name: "cell.csv", mimeType: "text/csv", buffer: Buffer.from("VALUE_KEY,NAME,DESCRIPTION,IS_ACTIVE\nCODE,Label,Description,sometimes") });
  await expect(dialog.getByText(/Red 2, kolona „IS_ACTIVE”/)).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Uvezi vrijednosti", exact: true })).toBeDisabled();
  expect(await state()).toEqual(before);
});
test("outage recovers honestly without an invented empty list", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await visit(page); await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible(); await expect(page.getByText(/Nema definisanih/)).toHaveCount(0); await expect(page.getByText(/private-fixture/)).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.getByLabel("Pretraga šifrarnika", { exact: false })).toBeEnabled(); await openFirst(page); await expect(page.getByRole("table")).toBeVisible();
});
test("375px editor, focus return and English resources remain usable", async ({ page }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await visit(page); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await openFirst(page); const trigger = page.getByRole("button", { name: "Nova vrijednost", exact: true }); await trigger.click(); const dialog = page.getByRole("dialog"); await settled(dialog);
  const bounds = await dialog.boundingBox(); expect(bounds!.x).toBeGreaterThanOrEqual(0); expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(375);
  expect(await dialog.locator(".rbi-modal__body").evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true); await page.screenshot({ path: info.outputPath("codes-mobile.png"), fullPage: true });
  await page.keyboard.press("Escape"); await expect(trigger).toBeFocused(); await page.getByRole("button", { name: "Prikaži ili sakrij navigaciju", exact: true }).click(); const nav = page.getByRole("dialog");
  await chooseLanguage(nav, "en");
  await expect(page.getByRole("heading", { name: "Code lists", exact: true })).toBeVisible(); await page.locator(".codes-card").first().click(); await page.getByRole("button", { name: "New value", exact: true }).click(); await expect(page.getByRole("dialog").getByRole("textbox", { name: /^Name/ })).toBeVisible();
});
test("category cards show names and leave audit details inside the selected codebook", async ({ page, request }) => {
  await request.post("/__fixtures/codes-reference", { headers });
  await visit(page);
  const card = page.getByRole("button", { name: "MigrationCodes", exact: true });
  await expect(card).toBeVisible();
  await expect(page.getByText("Nema definisanih šifrarnika.", { exact: true })).toHaveCount(0);
  await expect(page.getByText("Kreirao: fixture", { exact: true })).toHaveCount(0);
  await card.click();
  await expect(page.getByText("Kreirao: fixture", { exact: true })).toBeVisible();
});

test("LANGUAGE_PACKS exposes translation fields and saves changes by resource key", async ({ page, request }) => {
  expect((await request.post("/__fixtures/notifications-seed", { headers })).status()).toBe(204);
  await visit(page);
  await page.getByRole("button", { name: "LANGUAGE_PACKS", exact: true }).click();
  await page.getByRole("button", { name: "Struktura kolona", exact: true }).click();
  const structure = page.getByRole("dialog", { name: "Struktura kolona" });
  await expect(structure).toContainText("Ključ resursa");
  await expect(structure).toContainText("Ključ po kojem aplikacija pronalazi tekst.");
  await expect(structure).not.toContainText("Stable resource key");
  await structure.getByRole("button", { name: "Zatvori", exact: true }).click();
  const row = page.getByRole("row").filter({ hasText: "Rola dodijeljena" });
  await row.getByRole("button", { name: "Uredi vrijednost", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByLabel("Ključ resursa", { exact: false })).toHaveValue("notifications.fixture.title");
  await dialog.getByLabel("Prikazani tekst", { exact: false }).fill("Ažurirana poruka");
  await dialog.getByLabel("Tekst unutar polja", { exact: false }).fill("Unesite vrijednost");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).toHaveCount(0);
  const updated = page.getByRole("row").filter({ hasText: "Ažurirana poruka" });
  await expect(updated).toContainText("Unesite vrijednost");
});

test("duplicate definition shows an alert and allows retry with a new name", async ({ page }) => {
  await visit(page);
  await page.getByRole("button", { name: "Novi šifrarnik", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Novi šifrarnik" });
  const name = dialog.getByRole("textbox", { name: /^Naziv definicije/ });
  const submit = dialog.getByRole("button", { name: "Kreiraj", exact: true });
  await name.fill("osnov povezanosti");
  await configureColumn(dialog, "17", "Napomena", "1");
  await submit.click();
  await expect(dialog.getByText("Šifrarnik s tim nazivom već postoji.")).toBeVisible();
  await name.fill("testni sifrarnik");
  await submit.click();
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "TESTNI_SIFRARNIK", exact: true })).toBeVisible();
});

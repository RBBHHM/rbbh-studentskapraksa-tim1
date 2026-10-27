import { expect, test, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const fixtures = "../../artifacts/migration-browser-fixtures/";
async function visit(page: Page, category?: string) { await page.goto("/app/admin/code-lists"); await interactive(page); if (category) await page.getByLabel("Kategorija", { exact: true }).selectOption(category); }
async function settled(dialog: Locator) {
  await expect(dialog).toBeVisible(); await dialog.evaluate(async element => { await Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished.catch(() => {}))); });
  await expect(dialog).toHaveCSS("opacity", "1");
}
async function fillValue(page: Page, code: string, name: string) {
  await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill(name); await dialog.getByRole("textbox", { name: /^Kod/ }).fill(code); return dialog;
}
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });

test("real categories/history and native definition/value forms remain accessible", async ({ page }) => {
  await visit(page); await expect(page.getByRole("heading", { name: "Šifrarnici", exact: true })).toBeVisible();
  await expect(page.getByText(/Administrativni pregled uključuje neaktivne/)).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("button", { name: "Novi šifrarnik", exact: true }).click(); await settled(page.getByRole("dialog"));
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("dialog").getByRole("button", { name: "Odustani", exact: true }).click();
  await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click(); await settled(page.getByRole("dialog"));
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("create definition, value, immutable code edit and soft-delete preserve actual records and audit", async ({ page, request }) => {
  await visit(page); await page.getByRole("button", { name: "Novi šifrarnik", exact: true }).click(); let dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: /^Naziv definicije/ }).fill("NativeCodes"); await dialog.getByRole("textbox", { name: "Poslovna svrha", exact: true }).fill("Native fixture purpose");
  await dialog.getByRole("button", { name: "Kreiraj", exact: true }).click(); await expect(dialog).toHaveCount(0);
  await expect(page.getByLabel("Kategorija", { exact: true })).toHaveValue("NativeCodes"); await expect(page.getByText("Nema vrijednosti u odabranoj kategoriji.", { exact: true })).toBeVisible();
  dialog = await fillValue(page, "CODE_1", "Native value"); await dialog.getByRole("textbox", { name: "Redoslijed prikaza", exact: true }).fill("0");
  await dialog.getByRole("textbox", { name: "Opis", exact: true }).fill("Owned description"); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).toHaveCount(0);
  const row = page.getByRole("row").filter({ hasText: "CODE_1" }); await expect(row).toContainText("Native value"); await row.getByRole("button", { name: "Uredi vrijednost", exact: true }).click(); dialog = page.getByRole("dialog");
  await expect(dialog.getByRole("textbox", { name: /^Kod/ })).toBeDisabled(); await expect(dialog.getByRole("textbox", { name: "Redoslijed prikaza", exact: true })).toHaveValue("0");
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill("Edited value"); await dialog.getByRole("textbox", { name: "Redoslijed prikaza", exact: true }).fill("");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).toHaveCount(0); await expect(row).toContainText("Edited value");
  await row.getByRole("button", { name: "Obriši vrijednost", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Odustani", exact: true }).click(); await expect(row).toContainText("Aktivan");
  await row.getByRole("button", { name: "Obriši vrijednost", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click(); await expect(row).toContainText("Neaktivan");
  await page.reload(); await interactive(page); await page.getByLabel("Kategorija", { exact: true }).selectOption("NativeCodes"); await expect(row).toContainText("Neaktivan");
  const state = await (await request.get("/__fixtures/codes-state", { headers })).json();
  expect(state.values.find((item: any) => item.kod === "CODE_1")).toMatchObject({ kategorija: "NativeCodes", naziv: "Edited value", redoslijedPrikaza: null, aktivan: false, izmijenioKorisnik: "admin@fixture.test" });
  expect(state.audit.map((item: any) => item.action)).toEqual(["CODELIST_ADD", "CODELIST_ADD", "CODELIST_UPDATE", "CODELIST_DELETE"]); expect(state.audit.every((item: any) => item.username === "admin@fixture.test")).toBe(true);
});
test("changed reference value immediately reaches the actual business form dropdown", async ({ page }) => {
  await visit(page, "OsnovPovezanosti"); await page.getByRole("button", { name: "Uredi vrijednost", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill("Updated relationship basis"); await dialog.getByRole("textbox", { name: "Opis", exact: true }).fill("Updated reference description");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).toHaveCount(0);
  await page.goto("/app/legal-persons"); await interactive(page); await page.getByRole("button", { name: "Novi zapis", exact: true }).click();
  const business = page.getByRole("dialog"); const basis = business.getByLabel("Osnov povezanosti", { exact: false });
  await expect(basis.locator("option[value='ZOB-2-V-5']")).toHaveText("Updated relationship basis"); await basis.selectOption("ZOB-2-V-5");
  await expect(business.getByLabel("Opis povezanosti", { exact: false })).toHaveValue("Updated reference description");
});
test("validation blocks requests and duplicate code remains an authoritative backend rejection", async ({ page, request }) => {
  await visit(page, "OsnovPovezanosti"); await page.getByRole("button", { name: "Nova vrijednost", exact: true }).click(); let dialog = page.getByRole("dialog");
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog.locator("div[role=alert]").first()).toContainText("Naziv je obavezan");
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
  await dialog.getByRole("textbox", { name: /^Naziv/ }).fill("Duplicate attempt"); await dialog.getByRole("textbox", { name: /^Kod/ }).fill("ZOB-2-V-5");
  await dialog.getByRole("textbox", { name: "Redoslijed prikaza", exact: true }).fill("1.5"); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog.locator("div[role=alert]").first()).toContainText("nenegativan cijeli broj");
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
  await dialog.getByRole("textbox", { name: "Redoslijed prikaza", exact: true }).fill(""); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog.getByText("Backend je odbio podatke. Provjerite unesena polja.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/codes-state", { headers })).json()).audit).toEqual([]);
});
test("referenced value and entire category deletion are rejected with no data/audit change", async ({ page, request }) => {
  await request.post("/__fixtures/legal-existing", { headers }); await visit(page, "OsnovPovezanosti");
  const before = await (await request.get("/__fixtures/codes-state", { headers })).json();
  await page.getByRole("button", { name: "Obriši vrijednost", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click();
  await expect(page.getByText(/Brisanje je odbijeno:/)).toBeVisible(); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await page.getByRole("button", { name: "Obriši šifrarnik", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click(); await expect(page.getByText(/Brisanje je odbijeno:/)).toBeVisible();
  expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(before);
});
test("category soft-delete keeps inactive administrative history instead of pretending physical removal", async ({ page, request }) => {
  await request.post("/__fixtures/codes-reference", { headers }); await visit(page, "MigrationCodes");
  await page.getByRole("button", { name: "Obriši šifrarnik", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši", exact: true }).click(); await expect(page.getByText("Šifrarnik i njegove vrijednosti su obrisani.", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Kategorija", { exact: true })).toHaveValue("MigrationCodes"); expect(await page.getByRole("cell", { name: "Neaktivan", exact: true }).count()).toBe(2);
  const state = await (await request.get("/__fixtures/codes-state", { headers })).json(); expect(state.definitions).toEqual([{ name: "MigrationCodes", description: "Fixture purpose", isActive: false }]);
  expect(state.values.filter((item: any) => item.kategorija === "MigrationCodes").every((item: any) => !item.aktivan)).toBe(true); expect(state.audit.map((item: any) => item.action)).toEqual(["CODELIST_DELETE"]);
});
test("real XLSX preview does not persist; confirmed valid-row import preserves duplicate/error/code-collision behavior", async ({ page, request }) => {
  await request.post("/__fixtures/codes-reference", { headers }); await visit(page, "MigrationCodes");
  const before = await (await request.get("/__fixtures/codes-state", { headers })).json(); await page.locator("#code-file").setInputFiles(fixtures + "code-values.xlsx"); let dialog = page.getByRole("dialog"); await settled(dialog);
  await expect(dialog.getByText("Provjereno redova: 4; spremno: 2; postojećih: 1; upozorenja: 2.", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Red 5: naziv smije imati najviše 200 znakova.", { exact: true })).toBeVisible(); await expect(dialog.getByText("Redovi 3, 4: vrijednost 'New imported' je ponovljena u datoteci.", { exact: true })).toBeVisible();
  expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(before);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await dialog.getByRole("button", { name: "Odustani", exact: true }).click(); expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(before);
  await page.locator("#code-file").setInputFiles(fixtures + "code-values.xlsx"); dialog = page.getByRole("dialog"); await expect(dialog.getByRole("button", { name: "Uvezi ispravne redove", exact: true })).toBeEnabled();
  await dialog.getByRole("button", { name: "Uvezi ispravne redove", exact: true }).click(); await expect(dialog).toHaveCount(0); await expect(page.getByText("Uvezeno: 1 od 2 kandidata; postojećih: 1; upozorenja: 2. Provjerite osvježenu listu.", { exact: true })).toBeVisible();
  const state = await (await request.get("/__fixtures/codes-state", { headers })).json(); expect(state.values).toHaveLength(before.values.length + 1);
  expect(state.values.find((item: any) => item.kod === "NEW_IMPORTED")).toMatchObject({ naziv: "New imported", aktivan: true });
  expect(state.values.filter((item: any) => item.kod === "A_B")).toHaveLength(1); expect(state.audit.map((item: any) => item.action)).toEqual(["IMPORT"]); expect(JSON.parse(state.audit[0].newValues)).toMatchObject({ imported: 1, failed: 2, kategorija: "MigrationCodes" });
});
test("invalid extension, corrupt workbook and invalid header remain safe and create no records", async ({ page, request }) => {
  await visit(page); const before = await (await request.get("/__fixtures/codes-state", { headers })).json();
  await page.locator("#code-file").setInputFiles({ name: "wrong.txt", mimeType: "text/plain", buffer: Buffer.from("private-marker") }); await expect(page.getByText("Odaberite nepraznu .xlsx datoteku do 10 MB.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
  await page.locator("#code-file").setInputFiles({ name: "bad.xlsx", mimeType: "application/octet-stream", buffer: Buffer.from("private-marker") }); await expect(page.getByText(/Datoteka nije prihvaćena/)).toBeVisible();
  await page.locator("#code-file").setInputFiles(fixtures + "invalid-codes.xlsx"); await expect(page.getByText(/Datoteka nije prihvaćena/)).toBeVisible();
  expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(before); await expect(page.getByRole("dialog")).toHaveCount(0); await expect(page.getByText(/private-marker/)).toHaveCount(0);
});
test("outage recovers honestly without an invented empty list", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await visit(page); await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible(); await expect(page.getByText(/Nema definisanih/)).toHaveCount(0); await expect(page.getByText(/private-fixture/)).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.getByLabel("Kategorija", { exact: true })).toBeEnabled(); await expect(page.getByRole("table")).toBeVisible();
});
test("375px editor, focus return and English resources remain usable", async ({ page }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await visit(page); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const trigger = page.getByRole("button", { name: "Nova vrijednost", exact: true }); await trigger.click(); const dialog = page.getByRole("dialog"); await settled(dialog);
  const bounds = await dialog.boundingBox(); expect(bounds!.x).toBeGreaterThanOrEqual(0); expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(375);
  expect(await dialog.locator(".rbi-modal__body").evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true); await page.screenshot({ path: info.outputPath("codes-mobile.png"), fullPage: true });
  await page.keyboard.press("Escape"); await expect(trigger).toBeFocused(); await page.getByRole("button", { name: "Prikaži ili sakrij navigaciju", exact: true }).click(); const nav = page.getByRole("dialog");
  await chooseLanguage(nav, "en");
  await expect(page.getByRole("heading", { name: "Code lists", exact: true })).toBeVisible(); await page.getByRole("button", { name: "New value", exact: true }).click(); await expect(page.getByRole("dialog").getByRole("textbox", { name: /^Name/ })).toBeVisible();
});

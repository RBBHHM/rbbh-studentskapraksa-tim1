import { expect, test, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
async function visit(page: Page) { await page.goto("/app/admin/users"); await interactive(page); }
async function settled(dialog: Locator) {
  await expect(dialog).toBeVisible();
  await dialog.evaluate(async element => {
    await Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished.catch(() => {})));
  });
  await expect(dialog).toHaveCSS("opacity", "1");
}
async function access(dialog: Locator, name: string, selected: boolean) {
  const checkbox = dialog.getByRole("checkbox", { name, exact: true });
  await expect(checkbox).toBeVisible(); await expect(checkbox).toBeEnabled();
  if (await checkbox.isChecked() !== selected) { await checkbox.focus(); await checkbox.press("Space"); }
  await expect(checkbox).toBeChecked({ checked: selected });
}
async function createFields(page: Page, email = "new.user@raiffeisengroup.ba") {
  await page.getByRole("button", { name: "Dodaj korisnika", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Korisničko ime", { exact: false }).fill("new.user");
  await dialog.getByLabel(/^Ime/).fill("Native");
  await dialog.getByLabel("Prezime", { exact: false }).fill("User");
  await dialog.getByLabel("Email", { exact: false }).fill(email);
  return dialog;
}
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });

test("Admin sees real user/access metadata, accessible create form and disabled self-destructive controls", async ({ page }) => {
  await visit(page); const own = page.getByRole("row").filter({ hasText: "admin@fixture.test" });
  await expect(own.getByRole("button", { name: "Deaktiviraj", exact: true })).toBeDisabled();
  await expect(own.getByRole("button", { name: "Obriši korisnika", exact: true })).toBeDisabled();
  await expect(own.getByRole("button", { name: "Promijeni funkcionalne pristupe", exact: true })).toBeDisabled();
  await expect(own).toContainText("ADMINISTRATION_MANAGE");
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await page.getByRole("button", { name: "Dodaj korisnika", exact: true }).click();
  await settled(page.getByRole("dialog"));
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("create preserves multiple application roles and persists the actual backend record", async ({ page, request }) => {
  await visit(page); const dialog = await createFields(page);
  await dialog.getByText("Fizička lica", { exact: true }).click();
  await expect(dialog.getByRole("checkbox", { name: "Fizička lica", exact: true })).toBeChecked();
  await access(dialog, "Pravna lica", true);
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).toHaveCount(0);
  const row = page.getByRole("row").filter({ hasText: "new.user@raiffeisengroup.ba" });
  await expect(row).toContainText("FL_CREATE"); await expect(row).toContainText("PL_CREATE");
  await page.reload(); await interactive(page); await expect(row).toBeVisible();
  const state = await (await request.get("/__fixtures/users-state", { headers })).json();
  expect(state.users.find((user: { username: string }) => user.username === "new.user")).toMatchObject({ firstName: "Native", lastName: "User", isActive: true });
  expect(state.audit.map((entry: { action: string }) => entry.action)).toEqual(["CREATE"]);
  const duplicate = await createFields(page); await access(duplicate, "Fizička lica", true);
  await duplicate.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(duplicate.getByText("Backend je odbio podatke. Provjerite unesena polja.", { exact: true })).toBeVisible();
  const afterDuplicate = await (await request.get("/__fixtures/users-state", { headers })).json();
  expect(afterDuplicate.users).toHaveLength(state.users.length); expect(afterDuplicate.audit).toHaveLength(1);
});
test("required names, corporate email and create roles block requests", async ({ page, request }) => {
  await visit(page); await page.getByRole("button", { name: "Dodaj korisnika", exact: true }).click();
  let dialog = page.getByRole("dialog"); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Odaberite najmanje jedan funkcionalni pristup.", { exact: true }).first()).toBeVisible();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
  await dialog.getByRole("button", { name: "Odustani", exact: true }).click(); dialog = await createFields(page, "invalid@example.test");
  await access(dialog, "Fizička lica", true); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog.getByText("Unesite ispravnu email adresu s domenom @raiffeisengroup.ba.", { exact: true }).first()).toBeVisible();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
});
test("role replacement and non-final empty roles preserve the existing mapping-removal workflow", async ({ page, request }) => {
  await request.post("/__fixtures/users-reference", { headers }); await visit(page);
  let other = page.getByRole("row").filter({ hasText: "other-FL@fixture.test" });
  await other.getByRole("button", { name: "Promijeni funkcionalne pristupe", exact: true }).click();
  let dialog = page.getByRole("dialog"); await access(dialog, "Fizička lica", false);
  await access(dialog, "Kapital", true); await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click();
  await expect(dialog).toHaveCount(0); await expect(other).toContainText("KAPITAL_CREATE");
  await other.getByRole("button", { name: "Promijeni funkcionalne pristupe", exact: true }).click(); dialog = page.getByRole("dialog");
  await access(dialog, "Kapital", false);
  await expect(dialog.getByText(/postojeći aplikacijski zapis se uklanja/)).toBeVisible();
  await dialog.getByRole("button", { name: "Sačuvaj", exact: true }).click(); await expect(dialog).toHaveCount(0); await expect(other).toHaveCount(0);
});
test("own role editor is unavailable even when another active Admin exists", async ({ page, request }) => {
  await request.post("/__fixtures/users-reference", { headers }); await visit(page);
  const own = page.getByRole("row").filter({ hasText: "admin@fixture.test" }).filter({ hasNotText: "other-admin@fixture.test" });
  await expect(own.getByRole("button", { name: "Promijeni funkcionalne pristupe", exact: true })).toBeDisabled();
  await expect(own).toContainText("ADMINISTRATION_MANAGE");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByRole("row").filter({ hasText: "other-admin@fixture.test" }).getByRole("button", { name: "Promijeni funkcionalne pristupe", exact: true })).toBeEnabled();
  expect((await (await request.get("/__fixtures/users-state", { headers })).json()).audit).toEqual([]);
});
test("deactivate/reactivate and permanent delete require confirmation and preserve other Admin", async ({ page, request }) => {
  await request.post("/__fixtures/users-reference", { headers }); await visit(page);
  const other = page.getByRole("row").filter({ hasText: "other-admin@fixture.test" });
  await other.getByRole("button", { name: "Deaktiviraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Odustani", exact: true }).click(); await expect(other).toContainText("Aktivan");
  await other.getByRole("button", { name: "Deaktiviraj", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Deaktiviraj", exact: true }).click();
  await expect(other).toContainText("Neaktivan"); await other.getByRole("button", { name: "Reaktiviraj", exact: true }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Reaktiviraj", exact: true }).click(); await expect(other).toContainText("Aktivan");
  await other.getByRole("button", { name: "Obriši korisnika", exact: true }).click();
  await expect(page.getByRole("alertdialog")).toContainText("Keycloak identitet se ne briše");
  await page.getByRole("alertdialog").getByRole("button", { name: "Odustani", exact: true }).click(); await expect(other).toBeVisible();
  await other.getByRole("button", { name: "Obriši korisnika", exact: true }).click(); await page.getByRole("alertdialog").getByRole("button", { name: "Obriši korisnika", exact: true }).click(); await expect(other).toHaveCount(0);
  const state = await (await request.get("/__fixtures/users-state", { headers })).json(); expect(state.audit.map((entry: { action: string }) => entry.action)).toEqual(["DEACTIVATE", "REACTIVATE", "DELETE"]);
});
test("server paging exposes users beyond the source first twenty", async ({ page, request }) => {
  await request.post("/__fixtures/users-pages", { headers }); await visit(page);
  await page.getByRole("button", { name: "Sljedeća stranica", exact: true }).click(); await expect(page.getByText("Stranica 2 od 2; ukupno zapisa: 23.", { exact: true })).toBeVisible();
  await expect(page.getByRole("row").filter({ hasText: "page21@fixture.test" })).toBeVisible();
});
test("outage is redacted and retry recovers without fabricating empty users", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await visit(page);
  await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible(); await expect(page.getByText(/private-fixture/)).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByRole("row").filter({ hasText: "admin@fixture.test" })).toBeVisible();
});
test("375px user table/form and BS/EN owned copy remain usable", async ({ page }, info) => {
  await page.setViewportSize({ width: 375, height: 812 }); await visit(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole("button", { name: "Dodaj korisnika", exact: true }).click(); await expect(page.getByRole("dialog")).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const dialog = page.getByRole("dialog");
  await settled(dialog);
  expect(await dialog.locator(".rbi-modal__body").evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  const bounds = await dialog.boundingBox(); expect(bounds!.x).toBeGreaterThanOrEqual(0); expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(375);
  for (const input of await dialog.getByRole("textbox").all()) {
    const box = await input.boundingBox(); expect(box!.x).toBeGreaterThanOrEqual(bounds!.x); expect(box!.x + box!.width).toBeLessThanOrEqual(bounds!.x + bounds!.width);
  }
  await page.screenshot({ path: info.outputPath("users-mobile.png"), fullPage: true });
  await page.getByRole("dialog").getByRole("button", { name: "Odustani", exact: true }).click();
  await page.getByRole("button", { name: "Prikaži ili sakrij navigaciju", exact: true }).click();
  const nav = page.getByRole("dialog"); await nav.getByLabel("Jezik", { exact: true }).selectOption("en"); await nav.getByRole("button", { name: "Primijeni", exact: true }).click(); await interactive(page);
  await expect(page.getByRole("heading", { name: "User management", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Add user", exact: true }).click(); await expect(page.getByRole("dialog").getByText("Functional accesses", { exact: true })).toBeVisible();
});

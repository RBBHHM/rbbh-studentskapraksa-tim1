import { expect, test, type Page, type Locator } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
async function visit(page: Page) { await page.goto("/app/admin/period"); await interactive(page); }
async function settled(dialog: Locator) {
  await expect(dialog).toBeVisible();
  await dialog.evaluate(async element => { await Promise.all(element.getAnimations({ subtree: true }).map(animation => animation.finished.catch(() => {}))); });
  await expect(dialog).toHaveCSS("opacity", "1");
}
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });

test("actual current/empty state and clarification form are accessible with approved bounds", async ({ page, request }) => {
  await visit(page); await expect(page.getByText("Period je otvoren", { exact: true })).toBeVisible();
  await expect(page.getByText("Nema zahtjeva za prikaz.", { exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await request.post("/__fixtures/period-reference", { headers }); await page.reload(); await interactive(page);
  await page.getByRole("button", { name: "Dostavi traženu dopunu", exact: true }).click(); const dialog = page.getByRole("dialog"); await settled(dialog);
  const input = dialog.getByRole("textbox"); await expect(input).toHaveAttribute("minlength", "10"); await expect(input).toHaveAttribute("maxlength", "490");
  await expect(dialog.getByText("Dopuna mora sadržavati od 10 do 490 znakova. Tekst se ne skraćuje.", { exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
for (const length of [9, 10, 490, 491, 500]) test(`native clarification boundary ${length} preserves backend state and full text`, async ({ page, request }) => {
  await request.post("/__fixtures/period-reference", { headers }); await visit(page);
  await page.getByRole("button", { name: "Dostavi traženu dopunu", exact: true }).click(); const dialog = page.getByRole("dialog");
  const input = dialog.getByRole("textbox"); await expect(input).toHaveAttribute("maxlength", "490");
  // Deliberately bypass the browser attribute only to prove independent native
  // form validation; the real backend is probed separately below as authority.
  if (length > 490) await input.evaluate(element => element.removeAttribute("maxlength"));
  await input.fill("x".repeat(length)); await expect(dialog.getByText(`Uneseno ${length} od najviše 490 znakova; najmanje 10 znakova teksta.`, { exact: true })).toBeVisible();
  await dialog.getByRole("button", { name: "Potvrdi", exact: true }).click();
  const accepted = length >= 10 && length <= 490;
  if (accepted) await expect(dialog).toHaveCount(0);
  else await expect(dialog.locator("div[role=alert]").first()).toContainText("Dopuna mora sadržavati od 10 do 490 znakova.");
  const state = await (await request.get("/__fixtures/period-state", { headers })).json();
  expect(state.requests).toHaveLength(accepted ? 2 : 1);
  if (accepted) {
    expect(state.requests[1].reason).toBe("[ODGOVOR] " + "x".repeat(length)); expect(state.requests[1].reason.length).toBeLessThanOrEqual(500);
    expect(state.requests[0].status).toBe("NEEDS_INFO"); expect(state.requests[1].status).toBe("PENDING");
    expect(state.audit).toEqual([{ action: "UNLOCK_REQUEST", recordId: state.requests[0].id, username: "admin@fixture.test" }]);
  } else {
    expect(state.audit).toEqual([]); expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
    const probe = await (await request.post("/__fixtures/period-response", { headers, data: { id: state.requests[0].id, message: "x".repeat(length) } })).json(); expect(probe.status).toBe(400);
    const after = await (await request.get("/__fixtures/period-state", { headers })).json(); expect(after.requests).toEqual(state.requests); expect(after.audit).toEqual([]);
  }
});
test("lock, request, ask-info, respond and approve/unlock retain real workflow and audit", async ({ page, request }) => {
  await visit(page); await page.getByRole("button", { name: "Zaključaj period", exact: true }).click();
  await expect(page.getByText("Period je zaključan", { exact: true })).toBeVisible();
  await page.getByRole("textbox", { name: /^Razlog zahtjeva/ }).fill("Potrebna korekcija podataka");
  await page.getByRole("button", { name: "Pošalji zahtjev", exact: true }).click();
  const original = page.getByRole("row").filter({ hasText: "Potrebna korekcija podataka" });
  await original.getByRole("button", { name: "Traži dopunu", exact: true }).click();
  let dialog = page.getByRole("dialog"); await dialog.getByRole("textbox").fill("Dostavite dodatne podatke"); await dialog.getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(original).toContainText("Potrebna dopuna"); await original.getByRole("button", { name: "Dostavi traženu dopunu", exact: true }).click();
  dialog = page.getByRole("dialog"); await dialog.getByRole("textbox").fill("Potpuna trazena dopuna"); await dialog.getByRole("button", { name: "Potvrdi", exact: true }).click();
  const response = page.getByRole("row").filter({ hasText: "[ODGOVOR] Potpuna trazena dopuna" }); await expect(response).toContainText("Čeka obradu");
  await response.getByRole("button", { name: "Odobri/otključaj", exact: true }).click(); await expect(response).toContainText("Odobren"); await expect(original).toContainText("Potrebna dopuna");
  await expect(page.getByText("Period je otvoren", { exact: true })).toBeVisible();
  const state = await (await request.get("/__fixtures/period-state", { headers })).json(); expect(state.locks[0].isLocked).toBe(false);
  expect(state.audit.map((item: { action: string }) => item.action)).toEqual(["PERIOD_LOCK", "UNLOCK_REQUEST", "UNLOCK_REQUEST_NEEDS_INFO", "UNLOCK_REQUEST", "PERIOD_UNLOCK"]);
});
test("reject, status filter and cancel do not fabricate or duplicate mutations", async ({ page, request }) => {
  await request.post("/__fixtures/period-pages", { headers }); await visit(page);
  const row = page.getByRole("row").filter({ hasText: "requester000" }); await row.getByRole("button", { name: "Odbij", exact: true }).click();
  let dialog = page.getByRole("dialog"); await dialog.getByRole("button", { name: "Odustani", exact: true }).click();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
  await row.getByRole("button", { name: "Odbij", exact: true }).click(); dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox").fill("Nepotpuna dokumentacija"); await dialog.getByRole("button", { name: "Potvrdi", exact: true }).click(); await expect(row).toContainText("Odbijen");
  await page.getByLabel("Status", { exact: true }).selectOption("REJECTED"); await expect(page.getByRole("row")).toHaveCount(2);
  await expect(row).toContainText("Nepotpuna dokumentacija");
});
test("request paging reaches beyond the source first hundred", async ({ page, request }) => {
  await request.post("/__fixtures/period-pages", { headers }); await visit(page); await page.getByRole("button", { name: "Sljedeća stranica", exact: true }).click();
  await expect(page.getByText("Stranica 2 od 2; ukupno zapisa: 101.", { exact: true })).toBeVisible(); await expect(page.getByRole("row").filter({ hasText: "requester100" })).toBeVisible();
});
test("outage never looks like an open period and retry recovers", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await visit(page); await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("Period je otvoren", { exact: true })).toHaveCount(0); await expect(page.getByText(/private-fixture/)).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click(); await expect(page.getByText("Period je otvoren", { exact: true })).toBeVisible();
});
test("post-persist failure warns about uncertainty and refresh does not duplicate the response", async ({ page, request }) => {
  await request.post("/__fixtures/period-reference", { headers }); await visit(page); await request.post("/__fixtures/period-audit-fails", { headers });
  await page.getByRole("button", { name: "Dostavi traženu dopunu", exact: true }).click(); const dialog = page.getByRole("dialog");
  await dialog.getByRole("textbox").fill("Potpuna trazena dopuna"); await dialog.getByRole("button", { name: "Potvrdi", exact: true }).click();
  await expect(dialog.getByText(/Promjena je možda već snimljena/)).toBeVisible();
  let state = await (await request.get("/__fixtures/period-state", { headers })).json(); expect(state.requests).toHaveLength(2); expect(state.audit).toEqual([]);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(1);
  await request.post("/__fixtures/period-audit-restored", { headers }); await dialog.getByRole("button", { name: "Odustani", exact: true }).click();
  await page.reload(); await interactive(page); await expect(page.getByRole("row").filter({ hasText: "[ODGOVOR] Potpuna trazena dopuna" })).toBeVisible();
  state = await (await request.get("/__fixtures/period-state", { headers })).json(); expect(state.requests).toHaveLength(2);
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(1);
});
test("375px clarification dialog and BS/EN limit/count wording remain usable", async ({ page, request }, info) => {
  await request.post("/__fixtures/period-reference", { headers }); await page.setViewportSize({ width: 375, height: 812 }); await visit(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole("button", { name: "Dostavi traženu dopunu", exact: true }).click(); let dialog = page.getByRole("dialog"); await settled(dialog);
  const bounds = await dialog.boundingBox(); expect(bounds!.x).toBeGreaterThanOrEqual(0); expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(375);
  expect(await dialog.locator(".rbi-modal__body").evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("period-mobile.png"), fullPage: true }); await dialog.getByRole("button", { name: "Odustani", exact: true }).click();
  await page.getByRole("button", { name: "Prikaži ili sakrij navigaciju", exact: true }).click(); const nav = page.getByRole("dialog");
  await chooseLanguage(nav, "en");
  await expect(page.getByRole("heading", { name: "Period management", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Provide requested information", exact: true }).click(); dialog = page.getByRole("dialog"); await expect(dialog.getByText("Enter a clarification of 10 to 490 characters. Text is not truncated.", { exact: true })).toBeVisible();
});

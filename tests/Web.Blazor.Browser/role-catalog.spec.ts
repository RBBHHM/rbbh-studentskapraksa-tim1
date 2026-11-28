import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";

const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
const admin = role === "admin";
const headers = { "X-Fixture-Control": "true" };

test.beforeEach(async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204);
});

test("role catalog is visible only to administrators", async ({ page }) => {
  await page.goto("/app"); await interactive(page);
  await expect(page.locator("aside a[href='/app/admin/role-catalog']")).toHaveCount(admin ? 1 : 0);
  await page.goto("/app/admin/role-catalog"); await interactive(page);
  if (!admin) {
    await expect(page.getByRole("main").getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Nova rola" })).toHaveCount(0);
  } else {
    await expect(page.getByRole("heading", { name: "Katalog rola" })).toBeVisible();
    await expect(page.getByRole("table").getByText("Administrator", { exact: true })).toBeVisible();
    await expect(page.getByRole("table").getByText("Sistemska rola – zaštićena").first()).toBeVisible();
  }
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});

test("admin can create a DB-backed role with selected permission and search for it", async ({ page }) => {
  test.skip(!admin, "Requires actual database-owned admin grant");
  await page.goto("/app/admin/role-catalog"); await interactive(page);
  await page.getByRole("button", { name: "Nova rola" }).click();
  await page.locator("#rolecatalog-name").fill("qa_custom");
  const dialog = page.getByRole("dialog", { name: "Nova rola" });
  await dialog.getByRole("tab", { name: "Dozvole", exact: true }).click();
  const firstPermission = dialog.getByRole("checkbox").first();
  await firstPermission.press("Space");
  await expect(firstPermission).toBeChecked();
  const group = dialog.getByRole("combobox", { name: "Grupa dozvola" });
  await group.press("Tab");
  await page.keyboard.press("Shift+Tab");
  await expect(group).toBeFocused();
  expect(await group.evaluate(element => {
    const style = getComputedStyle(element);
    return parseFloat(style.borderLeftWidth) > 0 && parseFloat(style.outlineOffset) < 0;
  })).toBe(true);
  const firstModule = await group.inputValue();
  const lastModule = await group.locator("option").last().getAttribute("value");
  expect(lastModule).not.toBe(firstModule);
  await group.selectOption(lastModule!);
  await group.selectOption(firstModule);
  await expect(firstPermission).toBeChecked();
  await dialog.getByRole("tab", { name: "Detalji", exact: true }).click();
  await expect(dialog.locator("#rolecatalog-name")).toHaveValue("qa_custom");
  await page.getByRole("button", { name: "Sačuvaj" }).click();
  await expect(page.getByRole("status").getByText("Rola je uspješno sačuvana.")).toBeVisible();
  await page.locator("#rolecatalog-search").fill("qa_custom");
  await expect(page.getByRole("table").getByText("qa_custom", { exact: true })).toBeVisible();
  await page.getByRole("row", { name: /qa_custom/ }).getByRole("button", { name: "Obriši" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Obriši" }).click();
  await expect(page.getByRole("status").getByText("Rola je obrisana.")).toBeVisible();
  await expect(page.getByRole("row", { name: /qa_custom/ })).toHaveCount(0);
});

test("duplicate role shows an alert and fields adapt to smaller screens", async ({ page }) => {
  test.skip(!admin, "Requires actual database-owned admin grant");
  await page.goto("/app/admin/role-catalog"); await interactive(page);
  await page.getByRole("button", { name: "Nova rola" }).click();
  const dialog = page.getByRole("dialog", { name: "Nova rola" });
  const columns = dialog.locator(".role-catalog-permission-grid");
  await dialog.getByRole("tab", { name: "Dozvole", exact: true }).click();
  await expect(columns).toHaveCSS("grid-template-columns", /^\S+ \S+$/);
  await dialog.getByRole("tab", { name: "Detalji", exact: true }).click();
  await dialog.locator("#rolecatalog-name").fill("FL");
  await dialog.getByRole("button", { name: "Sačuvaj" }).click();
  await expect(dialog.getByRole("alert")).toHaveText("Rola s odabranim nazivom već postoji.");
  await page.setViewportSize({ width: 390, height: 620 });
  await dialog.locator("#rolecatalog-name").fill("nova_qa_rola");
  await expect(dialog.getByRole("alert")).toHaveCount(0);
  await dialog.getByRole("tab", { name: "Dozvole", exact: true }).click();
  await expect(columns).toHaveCSS("grid-template-columns", /^\S+$/);
  const group = dialog.getByRole("combobox", { name: "Grupa dozvola" });
  for (const option of await group.locator("option").evaluateAll(nodes => nodes.map(node => (node as HTMLOptionElement).value))) {
    await group.selectOption(option);
    await expect.poll(() => dialog.locator(".rbi-modal__body").evaluate(node =>
      node.scrollHeight <= node.clientHeight && node.scrollWidth <= node.clientWidth)).toBe(true);
  }
  await expect(dialog.getByRole("button", { name: "Sačuvaj" })).toBeInViewport();
  await dialog.getByRole("button", { name: "Odustani", exact: true }).click();
  expect(await page.locator(".rbi-table-scroll").evaluate(node => node.scrollWidth <= node.clientWidth)).toBe(true);
});

test("a referenced role is retained after deactivation", async ({ page, request }) => {
  test.skip(!admin, "Requires database-owned admin grant");
  expect((await request.post("/__fixtures/role-requests-seed", { headers })).status()).toBe(204);
  await page.goto("/app/admin/role-catalog");
  await interactive(page);
  const row = page.getByRole("row", { name: /Fizička lica/ });
  await expect(row.getByText(/Brisanje je nedostupno/)).toBeVisible();
  await expect(row.getByRole("button", { name: "Obriši" })).toHaveCount(0);
  await row.getByRole("button", { name: "Uredi rolu" }).click();
  const dialog = page.getByRole("dialog");
  const active = dialog.getByRole("checkbox", { name: "Aktivna rola" });
  await active.press("Space");
  await expect(active).not.toBeChecked();
  await dialog.getByRole("button", { name: "Sačuvaj" }).click();
  await expect(row.getByText(/Deaktivirana rola ostaje u katalogu/)).toBeVisible();
  await expect(row.getByRole("button", { name: "Obriši" })).toHaveCount(0);
});

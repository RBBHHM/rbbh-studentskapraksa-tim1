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
    await expect(page.getByRole("table").getByText("admin", { exact: true })).toBeVisible();
    await expect(page.getByRole("table").getByText("Sistemska rola – zaštićena").first()).toBeVisible();
  }
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});

test("admin can create a DB-backed role with selected permission and search for it", async ({ page }) => {
  test.skip(!admin, "Requires actual database-owned admin grant");
  await page.goto("/app/admin/role-catalog"); await interactive(page);
  await page.getByRole("button", { name: "Nova rola" }).click();
  await page.locator("#rolecatalog-name").fill("qa_custom");
  await page.locator("#rolecatalog-display-name").fill("QA Custom Role");
  await page.locator("#rolecatalog-description").fill("Automated test role");
  await page.locator("input[id^='rolecatalog-permission-']").first().check();
  await page.getByRole("button", { name: "Sačuvaj" }).click();
  await expect(page.getByRole("status").getByText("Rola je uspješno sačuvana.")).toBeVisible();
  await page.locator("#rolecatalog-search").fill("qa_custom");
  await page.getByRole("button", { name: "Pretraži", exact: true }).click();
  await expect(page.getByRole("table").getByText("qa_custom", { exact: true })).toBeVisible();
  await expect(page.getByRole("table").getByText("QA Custom Role", { exact: true })).toBeVisible();
});

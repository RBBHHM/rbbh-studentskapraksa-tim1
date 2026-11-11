import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";

const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204);
});
test("release notes are available to every authenticated role with UAT and PROD switching", async ({ page }) => {
  await page.goto("/app"); await interactive(page);
  await expect(page.locator("aside a[href='/app/release-notes']")).toBeVisible();
  await page.goto("/app/release-notes"); await interactive(page);
  await expect(page.getByRole("heading", { name: "Novosti u verziji" })).toBeVisible();
  await expect(page.getByText("UAT test", { exact: true })).toBeVisible();
  await expect(page.getByText("Production notes", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "PROD", exact: true }).click();
  await expect(page.getByText("Production notes", { exact: true })).toBeVisible();
  await expect(page.getByText("UAT test", { exact: true })).toHaveCount(0);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});

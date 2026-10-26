import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test("ordinary authenticated user reads empty/existing capital and downloads but cannot edit", async ({ page, request }) => {
  expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204);
  await page.goto("/app/capital"); await interactive(page);
  await expect(page.getByText("Kapital nije postavljen.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Uredi kapital", exact: true })).toHaveCount(0);
  await expect(page.getByRole("dialog", { includeHidden: true })).toHaveCount(0);
  await request.post("/__fixtures/limit-reference", { headers }); await page.getByRole("button", { name: "Osvježi", exact: true }).click();
  await expect(page.getByRole("cell", { name: "1.234.567,89 KM", exact: true })).toBeVisible();
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click();
  expect((await pending).suggestedFilename()).toMatch(/^kapital-.*\.xlsx$/);
  expect(await (await request.get("/__fixtures/counts", { headers })).json()).toEqual({ persons: 1, audit: 0, mutations: 0 });
});

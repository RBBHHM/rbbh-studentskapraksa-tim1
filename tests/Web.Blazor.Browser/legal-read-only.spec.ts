import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
import path from "node:path";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204);
  expect((await request.post("/__fixtures/legal-existing", { headers })).status()).toBe(204);
});
test("Guest can read and download legal entities but has no mutation actions", async ({ page }) => {
  await page.goto("/app/legal-persons"); await interactive(page);
  await expect(page.getByRole("row", { name: /Existing Company 00/ })).toBeVisible();
  for (const name of ["Novi zapis", "Uvezi Excel", "Uredi", "Verificiraj", "Obriši"]) await expect(page.getByRole("button", { name, exact: true })).toHaveCount(0);
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click();
  expect((await pending).suggestedFilename()).toMatch(/^pravna-lica-.*\.xlsx$/);
});
test("hostile markup cannot turn Guest into a legal import proxy", async ({ page, request }) => {
  await page.goto("/app/legal-persons"); await interactive(page);
  // A Guest is never rendered a file control; there is no hidden markup to open or upload through.
  await expect(page.locator("input[type=file]")).toHaveCount(0);
  await expect(page.getByLabel("Uvezi Excel", { exact: true })).toHaveCount(0);
  expect(await (await request.get("/__fixtures/counts", { headers })).json()).toEqual({ persons: 1, audit: 0, mutations: 0 });
  expect(await (await request.get("/__fixtures/legal-record", { headers })).json()).toHaveLength(1);
});

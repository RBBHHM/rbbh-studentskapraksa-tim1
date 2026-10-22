import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test("Guest reads limits, derived capital and export but cannot mutate", async ({ page, request }) => {
  for (const action of ["reset", "legal-existing", "limit-reference", "limit-existing"]) expect((await request.post(`/__fixtures/${action}`, { headers })).status()).toBe(204);
  await page.goto("/app/limits"); await interactive(page);
  await expect(page.getByRole("row", { name: /Existing Company 00/ })).toBeVisible();
  for (const name of ["Novi zapis", "Uredi", "Obriši"]) await expect(page.getByRole("button", { name, exact: true })).toHaveCount(0);
  const pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Preuzmi Excel", exact: true }).click();
  expect((await pending).suggestedFilename()).toMatch(/^limiti-.*\.xlsx$/);
  expect(await (await request.get("/__fixtures/counts", { headers })).json()).toEqual({ persons: 1, audit: 0, mutations: 0 });
});

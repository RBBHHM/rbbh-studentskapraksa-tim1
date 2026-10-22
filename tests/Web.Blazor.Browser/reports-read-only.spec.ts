import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => {
  for (const action of ["reset", "legal-existing", "report-reference", "report-existing"]) expect((await request.post(`/__fixtures/${action}`, { headers })).status()).toBe(204);
});
test("ordinary authenticated user reads and downloads existing reports, but cannot generate", async ({ page, request }) => {
  await page.goto("/app/reports"); await interactive(page);
  await expect(page.getByRole("row", { name: /Dnevni.*fixture-author/ })).toBeVisible();
  await expect(page.getByRole("button", { name: /^Generiši/ })).toHaveCount(0); await expect(page.getByLabel("Godina", { exact: true })).toHaveCount(0);
  let pending = page.waitForEvent("download"); await page.getByRole("row", { name: /Dnevni/ }).getByRole("button", { name: "Preuzmi", exact: true }).click(); expect((await pending).suggestedFilename()).toMatch(/^izvjestaj_.*\.xlsx$/);
  pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Svi klijenti", exact: true }).click(); await pending;
  await page.getByLabel("Porezni broj ili FBA ID", { exact: true }).fill("100000");
  pending = page.waitForEvent("download"); await page.getByRole("button", { name: "Preuzmi klijenta", exact: true }).click(); await pending;
  const state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.records).toHaveLength(1); expect(state.attempts).toBe(0);
  // Existing read/download audit writes are preserved, not confused with new
  // persisted business report generation.
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).audit).toBe(3);
});
test("profile lookup failure disables generation without removing authenticated archive/download access", async ({ page, request }) => {
  await request.post("/__fixtures/profile-unavailable", { headers }); await page.goto("/app/reports"); await interactive(page);
  await expect(page.getByText("Dozvolu za generisanje trenutno nije moguće provjeriti.", { exact: false })).toBeVisible();
  await expect(page.getByRole("link", { name: "Regulatorno izvještavanje", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: /^Generiši/ })).toHaveCount(0);
  const pending = page.waitForEvent("download"); await page.getByRole("row", { name: /Dnevni/ }).getByRole("button", { name: "Preuzmi", exact: true }).click(); await pending;
  const state = await (await request.get("/__fixtures/reports", { headers })).json(); expect(state.records).toHaveLength(1); expect(state.attempts).toBe(0);
});

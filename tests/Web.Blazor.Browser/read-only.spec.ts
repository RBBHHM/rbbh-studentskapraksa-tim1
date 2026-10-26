import { expect, test } from "@playwright/test";
import { openPhysical } from "./readiness";
import path from "node:path";

test.beforeEach(async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers: { "X-Fixture-Control": "true" } })).status()).toBe(204);
});
test("ordinary authenticated Guest sees real existing data and download, not mutations", async ({ page }) => {
  await openPhysical(page);
  await expect(page.getByRole("row", { name: /ReadOnly Fixture/ })).toBeVisible();
  await expect(page.getByRole("button", { name: "Izvezi Excel", exact: true })).toBeVisible();
  for (const name of ["Novi zapis", "Uvezi Excel", "Uredi", "Verificiraj", "Deaktiviraj"])
    await expect(page.getByRole("button", { name, exact: true })).toHaveCount(0);
  const download = page.waitForEvent("download");
  await page.getByRole("button", { name: "Izvezi Excel", exact: true }).click();
  expect((await download).suggestedFilename()).toMatch(/\.xlsx$/i);
});

test("opening hidden markup cannot turn Guest into an import proxy", async ({ page, request }) => {
  await openPhysical(page);
  // Deliberately hostile DOM mutation, not a normal interaction or a force-click
  // to satisfy a UI test. Server-side event guards must still reject the file.
  const hidden = page.getByRole("dialog", { name: "Uvezi Excel", exact: true, includeHidden: true });
  await hidden.evaluate((element: HTMLDialogElement) => element.showModal());
  await hidden.getByLabel("Excel datoteka").setInputFiles(path.resolve("../../artifacts/migration-browser-fixtures/valid-physical.xlsx"));
  await expect(hidden.getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toHaveCount(1);
  await expect(hidden.getByRole("button", { name: "Uvezi Excel", exact: true, includeHidden: true })).toBeDisabled();
  await expect(hidden).not.toBeVisible();
  const counts = await (await request.get("/__fixtures/counts", { headers: { "X-Fixture-Control": "true" } })).json();
  expect(counts).toEqual({ persons: 1, audit: 0, mutations: 0 });
});

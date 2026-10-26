import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test("non-Admin remains denied native Users and real API even with forged provider grants", async ({ page, request }) => {
  await request.post("/__fixtures/reset", { headers });
  for (const forged of [false, true]) {
    if (forged) await request.post("/__fixtures/forge-provider-admin", { headers });
    await page.goto("/app/admin/users"); await interactive(page);
    await expect(page.getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Korisnici", exact: true })).toHaveCount(0);
    expect((await (await request.get("/__fixtures/users-status", { headers })).json()).status).toBe(403);
  }
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
});

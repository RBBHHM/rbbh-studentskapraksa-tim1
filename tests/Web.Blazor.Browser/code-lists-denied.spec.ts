import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test("non-Admin route/write denial retains actual DB codebook read permission despite forged provider grants", async ({ page, request }) => {
  await request.post("/__fixtures/reset", { headers }); await request.post("/__fixtures/forge-provider-admin", { headers });
  const before = await (await request.get("/__fixtures/codes-state", { headers })).json();
  await page.goto("/app/admin/code-lists"); await interactive(page); await expect(page.getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible(); await expect(page.getByRole("link", { name: "Šifrarnici", exact: true })).toHaveCount(0); await expect(page.getByRole("button", { name: "Nova vrijednost", exact: true })).toHaveCount(0);
  expect(await (await request.post("/__fixtures/codes-denied-probes", { headers })).json()).toEqual({ read: 200, writes: [403, 403, 403, 403, 403, 403] });
  expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(before);
});

import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test("non-Admin cannot read audit, including with forged provider Admin/permission/marker claims", async ({ page, request }) => {
  await request.post("/__fixtures/reset", { headers }); await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Revizijski trag", exact: true })).toHaveCount(0);
  expect((await (await request.get("/__fixtures/audit-requests", { headers })).json()).count).toBe(0);
  expect((await (await request.get("/__fixtures/audit-status", { headers })).json()).status).toBe(403);
  await request.post("/__fixtures/forge-provider-admin", { headers }); await page.reload(); await interactive(page);
  expect((await (await request.get("/__fixtures/front-identity", { headers })).json()).providerAdmin).toBe(true);
  await expect(page.getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Revizijski trag", exact: true })).toHaveCount(0);
  expect((await (await request.get("/__fixtures/audit-status", { headers })).json()).status).toBe(403);
  expect((await (await request.get("/__fixtures/audit-requests", { headers })).json()).count).toBe(2);
});

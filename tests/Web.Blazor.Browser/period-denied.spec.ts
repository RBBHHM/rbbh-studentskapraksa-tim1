import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test("period UI and mutations follow database period grants despite forged provider claims", async ({ page, request }) => {
  await request.post("/__fixtures/reset", { headers }); await request.post("/__fixtures/period-reference", { headers });
  for (const forged of [false, true]) {
    if (forged) await request.post("/__fixtures/forge-provider-admin", { headers });
    await page.goto("/app/admin/period"); await interactive(page);
    const canManage = process.env.BROWSER_FIXTURE_ROLE === "kapital";
    if (!canManage) await expect(page.getByText("Nemate dozvolu za ovu radnju.", { exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Upravljanje periodom", exact: true })).toHaveCount(canManage ? 1 : 0);
    const state = await (await request.get("/__fixtures/period-state", { headers })).json();
    const probe = await (await request.post("/__fixtures/period-response", { headers, data: { id: state.requests[0].id, message: "x".repeat(9) } })).json();
    expect(probe.status).toBe(canManage ? 400 : 403);
  }
  const state = await (await request.get("/__fixtures/period-state", { headers })).json(); expect(state.requests).toHaveLength(1); expect(state.audit).toEqual([]);
});

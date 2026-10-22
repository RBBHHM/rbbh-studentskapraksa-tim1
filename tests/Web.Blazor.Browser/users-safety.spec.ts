import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });

for (const action of ["deactivate", "delete"]) test(`server rejects self ${action} and native audit stays accessible`, async ({ page, request }) => {
  const state = await (await request.get("/__fixtures/users-state", { headers })).json();
  const result = await (await request.post("/__fixtures/users-mutation", { headers, data: { id: state.users[0].id, action } })).json();
  expect(result.status).toBe(400);
  const after = await (await request.get("/__fixtures/users-state", { headers })).json();
  expect(after.users).toEqual(state.users); expect(after.audit).toEqual([]);
  await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByText("Nema revizijskih zapisa za prikaz.", { exact: true })).toBeVisible();
});

for (const empty of [false, true]) test(`server rejects final Admin role removal, empty=${empty}`, async ({ page, request }) => {
  const state = await (await request.get("/__fixtures/users-state", { headers })).json();
  const roleIds = empty ? [] : [state.roles.find((role: { name: string }) => role.name === "FL").id];
  const result = await (await request.post("/__fixtures/users-mutation", { headers, data: { id: state.users[0].id, action: "roles", roleIds } })).json();
  expect(result.status).toBe(409);
  await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByText("Nema revizijskih zapisa za prikaz.", { exact: true })).toBeVisible();
});

test("allowed other-Admin deactivation produces a real audit entry visible in Blazor", async ({ page, request }) => {
  await request.post("/__fixtures/users-reference", { headers });
  const state = await (await request.get("/__fixtures/users-state", { headers })).json();
  const other = state.users.find((user: { email: string }) => user.email === "other-admin@fixture.test");
  const result = await (await request.post("/__fixtures/users-mutation", { headers, data: { id: other.id, action: "deactivate" } })).json();
  expect(result.status).toBe(200);
  const after = await (await request.get("/__fixtures/users-state", { headers })).json();
  expect(after.users.find((user: { id: string }) => user.id === other.id).isActive).toBe(false);
  expect(after.audit).toEqual([{ action: "DEACTIVATE", recordId: other.id, username: "admin@fixture.test" }]);
  await page.goto("/app/admin/audit"); await interactive(page);
  await expect(page.getByRole("cell", { name: "Deaktivacija korisnika", exact: true })).toBeVisible();
  await expect(page.getByRole("cell", { name: "admin@fixture.test", exact: true })).toBeVisible();
});

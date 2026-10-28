import { expect, test } from "@playwright/test";
const headers = { "X-Fixture-Control": "true" };
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
test("actual protected controller matrix rejects cross-module and forged-provider writes before business validation", async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204);
  expect((await request.post("/__fixtures/forge-provider-admin", { headers })).status()).toBe(204);
  const beforeCounts = await (await request.get("/__fixtures/counts", { headers })).json();
  const beforeUsers = await (await request.get("/__fixtures/users-state", { headers })).json();
  const beforeCodes = await (await request.get("/__fixtures/codes-state", { headers })).json();
  const response = await request.post("/__fixtures/authorization-matrix", { headers }); expect(response.status()).toBe(200);
  const result = await response.json();
  for (const [area, owner] of Object.entries({ physical: "FL", legal: "PL", limits: "limiti", capital: "kapital", period: "kapital", generate: "kapital", users: "admin", codes: "admin" })) {
    if (role === "admin" || role === owner) expect([400, 404], `${role}/${area} reaches real validation`).toContain(result[area]);
    else expect(result[area], `${role}/${area} is forbidden`).toBe(403);
  }
  expect(result.reportRead).toBe(200); expect(result.periodRead).toBe(200); expect(result.codesRead).toBe(200);
  expect(result.usersRead).toBe(role === "admin" ? 200 : 403); expect(result.auditRead).toBe(role === "admin" ? 200 : 403);
  const afterCounts = await (await request.get("/__fixtures/counts", { headers })).json(); expect(afterCounts.persons).toBe(beforeCounts.persons); expect(afterCounts.audit).toBe(beforeCounts.audit);
  expect(await (await request.get("/__fixtures/users-state", { headers })).json()).toEqual(beforeUsers);
  expect(await (await request.get("/__fixtures/codes-state", { headers })).json()).toEqual(beforeCodes);
  expect(await (await request.get("/__fixtures/capital", { headers })).json()).toEqual([]);
  expect((await (await request.get("/__fixtures/reports", { headers })).json()).records).toEqual([]);
});

import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const admin = (process.env.BROWSER_FIXTURE_ROLE ?? "FL") === "admin";
test.beforeEach(async ({ request }) => {
  expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204);
  expect((await request.post("/__fixtures/role-requests-seed", { headers })).status()).toBe(204);
});

test("a signed-in user requests a role from the header, sees it pending on the profile and cannot send a second one", async ({ page, request }) => {
  await page.goto("/app/guide"); await interactive(page);
  await page.getByRole("button", { name: "Zatraži rolu", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog.getByRole("heading", { name: "Zatraži rolu", exact: true })).toBeVisible();
  const send = dialog.getByRole("button", { name: "Pošalji zahtjev", exact: true });
  await expect(dialog.getByRole("checkbox", { name: "Gost" })).toHaveCount(0); // Guest is the baseline, never requestable
  await expect(send).toBeDisabled();
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
  await dialog.getByRole("checkbox").first().check(); await expect(send).toBeEnabled();
  await dialog.getByLabel("Napomena (neobavezno)").fill("Treba mi pristup za rad na projektu");
  await send.click();
  await expect(page.getByText("Zahtjev je poslan administratorima.", { exact: true })).toBeVisible();
  await expect(dialog).toHaveCount(0);
  const state = await (await request.get("/__fixtures/role-requests-state", { headers })).json();
  expect(state.requests.filter((item: { status: string }) => item.status === "pending")).toHaveLength(admin ? 2 : 2);
  await page.goto("/app/profile"); await interactive(page);
  await expect(page.getByRole("heading", { name: "Moji zahtjevi za role", exact: true })).toBeVisible();
  await expect(page.locator(".profile-requests__list li").first()).toContainText("Na čekanju");
  await page.getByRole("button", { name: "Promijeni rolu", exact: true }).click();
  await page.getByRole("dialog").getByRole("checkbox").first().check();
  await page.getByRole("dialog").getByRole("button", { name: "Pošalji zahtjev", exact: true }).click();
  await expect(page.getByText("Već imate zahtjev koji čeka odluku.", { exact: false })).toBeVisible();
});

if (admin) {
  test("administrator reviews the queue, approves with a note and the requester gets the role and a notice", async ({ page, request }) => {
    await page.goto("/app/admin/role-requests"); await interactive(page);
    const row = page.getByRole("row", { name: /requester/ });
    await expect(row).toBeVisible(); await expect(row).toContainText("Need FL for onboarding");
    expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
    await row.getByRole("button", { name: "Odobri", exact: true }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Napomena uz odluku (neobavezno)").fill("Odobreno");
    await dialog.getByRole("button", { name: "Odobri", exact: true }).click();
    await expect(page.getByText("Zahtjev je odobren.", { exact: true })).toBeVisible();
    const state = await (await request.get("/__fixtures/role-requests-state", { headers })).json();
    expect(state.requests.find((item: { username: string }) => item.username === "requester").status).toBe("approved");
    expect(state.granted).toEqual(["FL"]); expect(state.notices.length).toBeGreaterThan(0);
    await page.getByRole("button", { name: "Odobreno", exact: true }).click();
    await expect(page.getByRole("row", { name: /requester/ })).toContainText("Odobreno");
  });
  test("administrator rejects a request and nothing is granted", async ({ page, request }) => {
    await page.goto("/app/admin/role-requests"); await interactive(page);
    const row = page.getByRole("row", { name: /requester/ });
    await row.getByRole("button", { name: "Odbij", exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Odbij", exact: true }).click();
    await expect(page.getByText("Zahtjev je odbijen.", { exact: true })).toBeVisible();
    const state = await (await request.get("/__fixtures/role-requests-state", { headers })).json();
    expect(state.requests.find((item: { username: string }) => item.username === "requester").status).toBe("rejected"); expect(state.granted).toEqual([]);
  });
} else {
  test("non-administrators cannot open the review queue", async ({ page }) => {
    await page.goto("/app/admin/role-requests"); await interactive(page);
    await expect(page.getByText("Nemate dozvolu za ovu radnju.", { exact: false }).or(page.getByRole("alert")).first()).toBeVisible();
    await expect(page.getByRole("button", { name: "Odobri", exact: true })).toHaveCount(0);
  });
}

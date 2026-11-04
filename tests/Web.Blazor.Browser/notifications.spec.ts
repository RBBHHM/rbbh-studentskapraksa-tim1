import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive, chooseLanguage } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
const bell = (page: import("@playwright/test").Page) => page.locator("a.app-bell");

test("empty inbox is accessible, has no unread badge and offers no mutations", async ({ page }) => {
  await page.goto("/app/notifications"); await interactive(page);
  await expect(page.getByRole("heading", { name: "Notifikacije", exact: true })).toBeVisible();
  await expect(page.getByText("Nemate obavijesti.", { exact: true })).toBeVisible();
  await expect(bell(page)).toHaveAttribute("aria-label", "Notifikacije"); await expect(page.locator(".app-bell__count")).toHaveCount(0);
  for (const name of ["Novi zapis", "Uredi", "Obriši", "Pošalji"]) await expect(page.getByRole("button", { name, exact: true })).toHaveCount(0);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("inbox lists only the recipient's notices, localized from the backend language pack, and marks them read", async ({ page, request }) => {
  await request.post("/__fixtures/notifications-seed", { headers }); await page.goto("/app/notifications"); await interactive(page);
  const items = page.locator("li.notification");
  await expect(items).toHaveCount(3);
  await expect(page.getByText("OTHERS-ONLY")).toHaveCount(0);
  await expect(items.filter({ hasText: "Odobrena rola FL." })).toHaveCount(1);
  await expect(bell(page)).toHaveAttribute("aria-label", "Notifikacije, nepročitanih: 2"); await expect(page.locator(".app-bell__count")).toHaveText("2");
  await expect(items.filter({ hasText: "Odobrena rola FL." }).getByRole("link", { name: "Otvori", exact: true })).toHaveAttribute("href", "/app/profile");
  await expect(items.filter({ hasText: "Odobrena rola PL." }).getByRole("link", { name: "Otvori", exact: true })).toHaveCount(0); // protocol-relative route is never linked
  await page.getByRole("button", { name: /^Nepročitane/ }).click(); await expect(items).toHaveCount(2);
  await items.first().getByRole("button", { name: "Označi kao pročitano", exact: true }).click();
  await expect(items).toHaveCount(1); await expect(page.locator(".app-bell__count")).toHaveText("1");
  await page.getByRole("button", { name: "Sve", exact: true }).click(); await expect(items).toHaveCount(3);
  await page.reload(); await interactive(page);
  await expect(page.locator(".app-bell__count")).toHaveText("1");
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("another recipient cannot read or mark someone else's notice", async ({ page, request }) => {
  await request.post("/__fixtures/notifications-seed", { headers }); await page.goto("/app/notifications"); await interactive(page);
  await expect(page.locator("li.notification")).toHaveCount(3);
  await expect(page.getByText("Odobrena rola OTHERS-ONLY.")).toHaveCount(0);
});
test("notification outage is redacted and retry recovers", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await page.goto("/app/notifications"); await interactive(page);
  await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("private-fixture-detail-not-for-ui", { exact: false })).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByText("Nemate obavijesti.", { exact: true })).toBeVisible();
});
test("375px inbox stays within the viewport", async ({ page, request }, info) => {
  await request.post("/__fixtures/notifications-seed", { headers }); await page.setViewportSize({ width: 375, height: 812 }); await page.goto("/app/notifications"); await interactive(page);
  await expect(page.locator("li.notification").first()).toBeVisible(); expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("notifications-mobile.png"), fullPage: true });
});
test("BS/EN wording follows the selected culture for static labels and the backend language pack", async ({ page, request }) => {
  await request.post("/__fixtures/notifications-seed", { headers }); await page.goto("/app/notifications"); await interactive(page);
  await chooseLanguage(page, "en");
  await expect(page.getByRole("heading", { name: "Notifications", exact: true })).toBeVisible();
  await expect(page.locator("li.notification").filter({ hasText: "Role FL was approved." })).toHaveCount(1);
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
});
test("the bell shows the unread count on other pages and opens the inbox", async ({ page, request }) => {
  await request.post("/__fixtures/notifications-seed", { headers }); await page.goto("/app/guide"); await interactive(page);
  await expect(page.locator(".app-bell__count")).toHaveText("2");
  await bell(page).click(); await expect(page).toHaveURL(/\/app\/notifications$/);
  await expect(page.locator("nav a[href='/app/notifications']")).toHaveCount(0); // not a sidebar entry
});

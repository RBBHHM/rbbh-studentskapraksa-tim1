import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
test.beforeEach(async ({ request }) => { expect((await request.post("/__fixtures/reset", { headers })).status()).toBe(204); });
test("empty notification log is accessible and has no business mutations", async ({ page }) => {
  await page.goto("/app/notifications"); await interactive(page);
  await expect(page.getByRole("heading", { name: "Notifikacije", exact: true })).toBeVisible();
  await expect(page.getByText("Nema evidentiranih obavijesti za prikaz.", { exact: true })).toBeVisible();
  for (const name of ["Novi zapis", "Uredi", "Obriši", "Pošalji"]) await expect(page.getByRole("button", { name, exact: true })).toHaveCount(0);
  expect((await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze()).violations).toEqual([]);
});
test("actual recorded row retains metadata/body search without rendering HTML", async ({ page, request }) => {
  await request.post("/__fixtures/email-existing", { headers }); await page.goto("/app/notifications"); await interactive(page);
  const row = page.getByRole("row", { name: /Fixture notification/ }); await expect(row).toBeVisible();
  await expect(row.getByRole("cell", { name: "Evidentirano", exact: true })).toBeVisible();
  await page.getByLabel("Pretraži zapise", { exact: true }).fill("body-search-needle"); await expect(row).toBeVisible();
  expect(await page.evaluate(() => (window as unknown as Record<string, unknown>).__notificationExecuted)).toBeUndefined();
  await expect(page.locator('img[src="x"]')).toHaveCount(0);
  await page.getByLabel("Pretraži zapise", { exact: true }).fill("no-match"); await expect(row).toHaveCount(0);
  await page.getByLabel("Pretraži zapise", { exact: true }).fill("fixture@"); await expect(row).toBeVisible();
});
test("SMTP empty response is explicit, not a fabricated sent or delivered mailbox", async ({ page, request }) => {
  await request.post("/__fixtures/email-existing", { headers }); await request.post("/__fixtures/email-smtp", { headers });
  await page.goto("/app/notifications"); await interactive(page);
  await expect(page.getByText("SMTP isporuka se ne prikazuje", { exact: false })).toBeVisible(); await expect(page.getByRole("row", { name: /Fixture notification/ })).toHaveCount(0);
  await request.post("/__fixtures/email-demo", { headers }); await page.getByRole("button", { name: "Osvježi", exact: true }).click();
  await expect(page.getByRole("row", { name: /Fixture notification/ })).toBeVisible();
});
test("notification outage is redacted and retry recovers", async ({ page, request }) => {
  await request.post("/__fixtures/unavailable", { headers }); await page.goto("/app/notifications"); await interactive(page);
  await expect(page.getByText("Servis trenutno nije dostupan. Pokušajte ponovo.", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("private-fixture-detail-not-for-ui", { exact: false })).toHaveCount(0);
  await request.post("/__fixtures/available", { headers }); await page.getByRole("button", { name: "Pokušaj ponovo", exact: true }).click();
  await expect(page.getByText("Nema evidentiranih obavijesti za prikaz.", { exact: true })).toBeVisible();
});
test("375px notification table remains within its scroll region", async ({ page, request }, info) => {
  await request.post("/__fixtures/email-existing", { headers }); await page.setViewportSize({ width: 375, height: 812 }); await page.goto("/app/notifications"); await interactive(page);
  await expect(page.getByRole("row", { name: /Fixture notification/ })).toBeVisible(); expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath("notifications-mobile.png"), fullPage: true });
});
test("BS/EN labels and actual backend notification wording follow selected culture", async ({ page, request }) => {
  await request.post("/__fixtures/email-existing", { headers }); await page.goto("/app/notifications"); await interactive(page);
  await page.getByLabel("Jezik", { exact: true }).selectOption("en"); await expect(page.locator("form.app-language")).toHaveAttribute("action", /\/en\?/);
  await page.getByRole("button", { name: "Primijeni", exact: true }).click(); await interactive(page);
  await expect(page.getByRole("heading", { name: "Notifications", exact: true })).toBeVisible();
  await expect(page.getByRole("cell", { name: "Period lock/unlock notice to all users", exact: true })).toBeVisible();
  await expect(page.getByRole("cell", { name: "Recorded", exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/culture", { headers })).json()).language).toBe("en");
});

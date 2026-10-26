import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
const area = role === "PL" ? "legal-persons" : role === "limiti" ? "limits" : role === "kapital" || role === "admin" ? "capital" : "physical-persons";
for (const fault of ["lock", "period-unavailable"]) test(`${area} disables mutations for ${fault} while keeping reads available`, async ({ page, request }) => {
  await request.post("/__fixtures/reset", { headers });
  await request.post(`/__fixtures/${fault}`, { headers });
  await page.goto(`/app/${area}`); await interactive(page);
  for (const name of ["Novi zapis", "Uvoz", "Uredi", "Uredi kapital", "Verificiraj", "Deaktiviraj", "Obriši"]) {
    for (const button of await page.getByRole("main").getByRole("button", { name, exact: true }).all()) await expect(button).toBeDisabled();
  }
  await expect(page.getByRole("button", { name: "Preuzmi Excel", exact: true })).toBeEnabled();
  await expect(page.getByText(fault === "lock" ? "Period je zaključan. Izmjene nisu dozvoljene." : "Stanje perioda trenutno nije dostupno. Mutacija je onemogućena dok se stanje ne provjeri. Osvježite prikaz.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
});

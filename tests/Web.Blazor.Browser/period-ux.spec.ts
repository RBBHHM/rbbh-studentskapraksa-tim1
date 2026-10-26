import { expect, test } from "@playwright/test";
import { interactive } from "./readiness";
const headers = { "X-Fixture-Control": "true" };
const role = process.env.BROWSER_FIXTURE_ROLE ?? "FL";
const area = role === "PL" ? "legal-persons" : role === "limiti" ? "limits" : role === "kapital" || role === "admin" ? "capital" : "physical-persons";
for (const fault of ["lock", "period-unavailable"]) test(`${area} disables mutations for ${fault} while keeping reads available`, async ({ page, request }) => {
  await request.post("/__fixtures/reset", { headers });
  if (area === "legal-persons" || area === "limits")
    expect((await request.post("/__fixtures/legal-existing", { headers })).status()).toBe(204);
  if (area === "limits")
    for (const fixture of ["limit-reference", "limit-existing"])
      expect((await request.post(`/__fixtures/${fixture}`, { headers })).status()).toBe(204);
  await request.post(`/__fixtures/${fault}`, { headers });
  await page.goto(`/app/${area}`); await interactive(page);
  const actions = area === "capital" ? ["Uredi kapital"]
    : area === "physical-persons" ? ["Novi zapis", "Uvezi Excel"]
    : area === "legal-persons" ? ["Novi zapis", "Uvezi Excel", "Uredi", "Verificiraj", "Deaktiviraj"]
    : ["Novi zapis", "Uredi", "Obriši"];
  for (const name of actions) await expect(page.getByRole("main").getByRole("button", { name, exact: true })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Izvezi Excel", exact: true })).toBeEnabled();
  await expect(page.getByText(fault === "lock" ? "Period je zaključan. Izmjene nisu dozvoljene." : "Stanje perioda trenutno nije dostupno. Mutacija je onemogućena dok se stanje ne provjeri. Osvježite prikaz.", { exact: true })).toBeVisible();
  expect((await (await request.get("/__fixtures/counts", { headers })).json()).mutations).toBe(0);
});

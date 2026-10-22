import { expect, type Page } from "@playwright/test";

export async function interactive(page: Page) {
  // SSR buttons exist before Blazor can handle clicks. Wait for the real
  // package sidebar connection created by interactive OnAfterRender, not a sleep.
  await expect.poll(() => page.evaluate(async () => {
    const path = "/_content/RBI.DesignSystem/js/rbi-extended-controls.js";
    const module = await import(path);
    return module.activeConnectionCount();
  })).toBeGreaterThan(0);
}

export async function openPhysical(page: Page) {
  await page.goto("/app/physical-persons");
  await interactive(page);
}

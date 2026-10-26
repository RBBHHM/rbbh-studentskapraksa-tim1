import { expect, type Page } from "@playwright/test";

export async function interactive(page: Page) {
  // SSR buttons exist before Blazor can handle clicks. The application shell sets
  // data-interactive only from interactive OnAfterRender, i.e. once the circuit is connected.
  await expect(page.locator(".app-shell[data-interactive='true']")).toBeAttached();
}

export async function openPhysical(page: Page) {
  await page.goto("/app/physical-persons");
  await interactive(page);
}

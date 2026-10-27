import { expect, type Locator, type Page } from "@playwright/test";

export async function interactive(page: Page) {
  // SSR buttons exist before Blazor can handle clicks. The application shell sets
  // data-interactive only from interactive OnAfterRender, i.e. once the circuit is connected.
  await expect(page.locator(".app-shell[data-interactive='true']")).toBeAttached();
}

export async function openPhysical(page: Page) {
  await page.goto("/app/physical-persons");
  await interactive(page);
}

/** The language applies as soon as it is chosen (no separate apply button), so wait for the culture round trip. */
export async function chooseLanguage(scope: Page | Locator, culture: string) {
  const page = "page" in scope && typeof scope.page === "function" ? scope.page() : (scope as Page);
  const navigated = page.waitForNavigation({ waitUntil: "load" });
  await scope.getByLabel("Jezik", { exact: true }).selectOption(culture);
  await navigated;
  await interactive(page);
}
# Playwright tests

Visual regression (`tests/visual`) and accessibility (`tests/a11y`) suites for
the app's routes, run across locales and viewports.

## Run

```bash
bunx playwright test
```

Runs against `pnpm dev` on `http://localhost:8080` (started automatically;
set `reuseExistingServer` behaviour via `CI`).

Run just one suite:

```bash
bunx playwright test tests/visual
bunx playwright test tests/a11y
```

## Update snapshots

After an intentional visual change:

```bash
bunx playwright test tests/visual --update-snapshots
```

Commit the updated `.png` files under `tests/visual/routes.spec.ts-snapshots/`.

## Add a route

Add one entry to the `routes` array in `tests/visual/matrix.ts`. It is
automatically covered for every locale, every viewport (visual suite) and at
desktop width (a11y suite) — no other file needs to change.

## Notes

- Locale is set via the `rbi.locale` cookie, the same mechanism the app uses
  at runtime (see `src/localization/config/locale-resolution.ts`).
- Tests wait for the localization loading spinner to disappear before taking
  a snapshot, so screenshots don't capture the loading state.
- Config (`playwright.config.ts`) fixes timezone/locale/animations for
  deterministic screenshots across machines.

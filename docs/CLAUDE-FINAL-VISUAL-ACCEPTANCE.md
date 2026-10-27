# Final visual acceptance — native Blazor vs React reference

Base: `1147e97`. React reference: historical commit `ccca8c5` (worktree, `src/Web` still present; **not** restored into this branch).
Both apps were run side by side on the same seeded backend (Kestrel fixture host, port 5100; React dev server proxying to it; Blazor on 5273) and compared by rendered screenshots at 1440x900 and 375x812, light and dark. Screenshots are local evidence only (not committed).

## Resolved from the previous pass
| Previous difference | Resolution |
|---|---|
| Limits client picker | Replaced by a type-ahead combobox (debounced search, listbox, arrow/Enter/Escape, loading and no-results rows, selected-client chip with clear). Same component is the limit form entity field. |
| Capital `KM` | `BAM <n>` with en-GB number format (identical to the React render); ISO date in BS, dd/MM/yyyy in EN. |
| Guide wording | Reference five sections and wording restored; obsolete React rules (automatic Guest for every signed-in user, binding by e-mail) replaced by the current approved rules. |
| Refresh / Osvježi | Removed everywhere. Out-of-band refresh is a browser reload; tests use `page.reload()`. |
| Apply / Primijeni | Removed. Selecting a language submits the (antiforgery-protected) culture form immediately; culture persistence and reload are covered by tests. |
| Two-row mobile header | Single 64px row at 375px; system-theme button hidden so the picker matches the two-state React toggle. |

## Other changes in this pass
Excel import is now the React flow (file button → immediate import → result dialog). Physical form: step bar, segmented residency and yes/no controls, reference field order. Wide 72rem dialogs, 44px controls, validation box with icon. Code-list/user/capital dialog sizes. Status indicators and nav icons use lucide-equivalent glyphs through an app-owned `AppIcon` (RbiIconName lacks them). Users/period/code-list/profile/reports copy aligned.

## Deliberate differences that remain (accessibility / business behaviour)
* Form control borders keep RBI `--border-strong` (React uses `--border-subtle`, ≈1.2:1 on white) — WCAG 1.4.11.
* Inline per-field validation messages are kept in addition to the summary box (WCAG 3.3.1).
* Month/year on Reports remain `select`s (backend year bounds are asserted by tests); they show numeric months like the reference.
* Period empty state keeps a message card instead of a header-only table.
* Profile keeps the identity details block; Users role labels are the long names.
* RBI checkbox/radio glyphs and select chevrons are RBI components.

## Verification
Unit 210/210. Browser (workers=1, retries=0): admin 77, FL 37, PL 35, kapital 40, limiti 31, gost 27 = 247 passed, 0 failed; Axe and 375px cases are part of those suites. Release build of `Web.Blazor` succeeded, `git diff --check` clean.

Not exhaustively re-inspected side by side: every validation/duplicate/locked/read-only dialog variant and the dark 375px state of every route (spot-checked: dashboard, users, physical form).

## Second pass (after 2dd254f)
Fixed from side-by-side inspection: family tree (bordered nodes, nested guide line, indicator chips, option labels with person type); limits table (raw values, wrapping headers, Osnovni kapital column visible); reports (numeric year/month inputs bounded 1–12 / 2000–current, React headers, ISO date, small download button with icon, React notice copy); notifications column titles; users role labels (FL/PL), self-protected note visually hidden; period (header-only table, secondary unlock button, React column titles, lock metadata/help text visually hidden); profile (extra identity block removed, React heading scale); toasts for success notices (bottom-right, auto-leave); dark-mode filled fields; legal form field order; code-list definition dialog submit "Kreiraj"; guide/profile spacing.

### Inspected side by side (local screenshots, not committed)
* Desktop light + dark and 375px light + dark: all 13 routes (one pass each, composites).
* Dialog/state pairs: physical create (resident/non-resident, step 1 validation, duplicate identity, step 2, edit), physical family tree, verify action (React has no confirmation), limits typeahead (empty/populated/keyboard/chip) and create with entity panel, legal create non-resident, capital edit + validation, reports daily/monthly generation, users create/validation, period lock state, code-list definition dialog and delete-value confirmation, populated vs empty states for most list pages.

### NOT inspected side by side in this pass
Legal duplicate/verify/delete confirmations, import success/error result dialog vs React, users edit/deactivate/reactivate/delete confirmations for a non-self user (React self row is not protected, Blazor is), period request/info/respond/reject/approve dialogs (need a non-admin and request fixtures), code-list preview/duplicate/partial-import dialogs, notifications SMTP/search states, audit pagination, profile backend-unavailable, locked/read-only variants of every form, and 375px dark/light for most dialogs. These are therefore not claimed EXACT.

### Known remaining differences
* Verify action keeps a confirmation dialog (React verifies immediately and shows a toast); row tooltip says "Deaktiviraj" where React says "Obriši".
* Self-protected users row stays disabled (security requirement; React allows self-management).
* Inline field messages and the summary box both appear (WCAG 3.3.1); input borders keep RBI strong border (WCAG 1.4.11).
* React shows raw API timestamps in some tables; native keeps readable formatting for audit/notifications.
* Code-list header actions wrap at 375px (React overflows and clips them).
## Third pass (after 896cea9)
Product decisions applied: verify runs immediately with a React-style toast (failure → danger toast; period lock stays authoritative server-side); delete wording/confirmation copy "Obriši"/"Obrisati zapis?" for persons, legal entities and limits (backend soft-deactivation unchanged); React toast copy per action; audit/notification timestamps and report date rendered like the reference (BS "yyyy M10 dd HH:mm", EN "dd MMM yyyy, HH:mm", reports raw ISO); users edit/delete dialog copy, row tooltips and bold name cell; period note dialog (compact RBI dialog, description, counter, confirm disabled below 10 characters), period action buttons (small, destructive reject), request form panel; audit/notification no-match copy; SMTP banner kept for assistive tech only; checkbox sizing in dialogs.

### Intentional differences
* INTENTIONAL_ACCESSIBILITY_DIFFERENCE: input borders (WCAG 1.4.11); field message + summary box (WCAG 3.3.1); wrapping code-list header actions at 375px; required-field asterisks.
* INTENTIONAL_SECURITY_DIFFERENCE: self-protected users row stays disabled; access-denied presentation follows the approved authorization model (Guest does not see admin navigation); import errors are redacted/localized instead of echoing backend text.
* Data completeness: audit keeps server paging with a pager (React shows only the first page without a pager).

### Still not inspected side by side
Legal delete result and read-only/locked variants of every form at 375px, code-list XLSX preview/duplicate/partial-import dialogs (covered by browser tests only), profile backend-unavailable, capital empty, reports failure/unavailable, limits locked, most dark/375px dialog variants. These are not claimed EXACT.

### Verification
Unit 210/210. Browser, workers=1, retries=0, six full suites on the final code: admin 77, FL 37, PL 35, kapital 40, limiti 31, gost 27 = 247 passed. Release build of Web.Blazor: 0 warnings, 0 errors. `git diff --check` clean.
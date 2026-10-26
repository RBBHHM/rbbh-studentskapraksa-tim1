# React → Blazor visual parity pass

Branch `ai/claude-visual-parity`, base `18702db`. React (`src/Web`, webpack dev server) and native Blazor (`src/Web.Blazor`) were run side by side against the same seeded backend (Kestrel harness built from `tests/Web.Blazor.BrowserHost`, kept outside the repository) and compared at 1440×900, light theme.

## Root cause
The generated template shell (`RbiSidebar` rail with the title inside it, `HeroBar` as a bare heading, `RbiCard` grid dashboard, plain tables with text buttons) shared tokens with the React product but none of its composition: no top bar with logo, no grouped navigation, no yellow active state, no brand hero, no bento dashboard cards, no icon indicators or icon actions, a different page-header pattern.

## What changed (RBI components + app-owned CSS only)
- Shell: sticky header (`RbiLogo`, product name, `RbiThemePicker`, language, profile link, sign-out), 17rem aside with *Workspace / Administration* groups, per-module icons, yellow active item, guide pinned at the bottom, mobile menu toggle that opens the aside as a dialog containing the language switcher.
- Dashboard: brand hero (hatch, display title, proof line) and bento cards (icon tile, arrow, hover) for the six work areas.
- Page header pattern (`HeroBar`): optional eyebrow (resource pages only, as in React), title, description, actions on the right; Capital uses the icon variant.
- Registry pages (physical, legal, limits, notifications, audit, users, code lists, capital, period, reports): search field with icon, bordered table surface, `Indicator` circles for type/residency/status/active, icon-only row actions (accessible names unchanged), centered empty state with React copy, family tree section, reports control panel, period status card, profile hero + role/permission cards, guide hero/legend/numbered steps.
- Copy aligned with React: nav titles, page descriptions, "Uvezi/Izvezi Excel", empty-state text.

## Test changes (reference copy / new shell, no assertion weakened)
Renamed labels (Export/Import Excel, Notifications/Users/Audit log/Individuals, Overview), empty state asserted as heading "Nema zapisa", tree select label "Fizičko lice", dashboard test split into dashboard cards (main) and shell navigation, readiness now waits for `data-interactive` set by the layout in `OnAfterRender` (the previous check counted `RbiSidebar` JS connections, which no longer exist).

## Verification status
- Blazor unit tests: 210/210.
- Browser suite, role `admin`: 74/75 on the last full run; the one failure (language/home test) was fixed afterwards but **not re-run**.
- Roles FL, PL, kapital, limiti, gost: **not run** (the run was stopped by the system for low memory).
- Dark theme, 375px visual comparison, axe on the new markup: **not performed visually**; only the existing 375px/axe assertions in the admin suite ran.

## Route status
| Route | Status | Notes |
|---|---|---|
| /app | NEAR-EXACT | currency of cards n/a; hatch is CSS |
| physical, legal | NEAR-EXACT | residency/draft icons differ (no Home/Clock icons in RBI set); extra *Osvježi* button |
| limits | MINOR DIFFERENCE | client filter is the existing server-side picker, restyled, not React's typeahead |
| capital | MINOR DIFFERENCE | currency shown as KM (culture) vs BAM |
| reports, period, code lists, users, audit, notifications, profile, guide | NEAR-EXACT / MINOR | guide text differs in wording; users action icons approximate (Settings/Lock/RotateCcw) |

## Design-system limitations
`RbiIconName` lacks Scale, Landmark, FileChartColumn, CalendarClock, BookOpen, ScrollText, Briefcase, Home, Clock, Mail, key and power icons; closest icons were used (Wallet, FileText, Lock, Folder, ShieldCheck, TrendingUp…). `RbiSidebar` cannot sit below a full-width header, so the aside is composed in the app.

## Not changed on purpose
Frontend permission visibility (`reports.read`, period for kapital) still follows the current Blazor contract; React lets *kapital* see Reports/Notifications/Period by role and the dashboard has no admin cards. Backend, schema, lock semantics, workflows untouched.

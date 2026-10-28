# Native business code-list administration

2026-10-23. Local module closure; not SQL/SSO/OCP acceptance or cutover.

`/app/admin/code-lists` uses the current DB-backed Admin profile and
CODE_LISTS_VIEW/ADMINISTRATION_MANAGE. The API's existing read permission for
other authenticated application roles is preserved; the Admin-only UI is not
mistaken for a new API read restriction. Forged provider roles/permissions do
not authorize any mutation. These business codebooks are not localization packs.

Typed source contracts preserve categories, complete active/inactive values,
creation, immutable code/category during edit, nullable/zero integer order,
descriptions, activity and modification actor/time. Dedicated native forms use
RBI fields, validation and dialogs. Inputs respect existing storage lengths;
backend validation remains authoritative. No generic metadata CRUD, DB access,
financial calculation or Excel parser was added to the frontend.

Deletion retains the backend's reference checks and soft-delete semantics.
Deleting a populated category can leave its inactive values/category visible
in administrative history, exactly as the actual API specifies. The native UI
does not falsely pretend those records were physically removed. Existing
update/activity behavior is not replaced by a newly invented business policy.

## Confirmed HTTP compatibility defect and narrow correction

The initial browser run was 8 PASS / 1 FAIL: deletion of a referenced category
left data intact but returned HTTP200 and the UI announced success. In
CodeListController, returning `ActionResult<bool>` or
`ActionResult<CodeListResponseDTO>` from an `ActionResult<object>` action wrapped
the rejected action as a successful serialized object. Direct real HTTP tests
confirmed four red cases: referenced-category400, missing-category404,
invalid-update400 and missing-update404 all incorrectly returned200. The fifth
invalid-read case already returned400 through MVC validation.

Only the two proven mutation error branches now return the existing helper's
HTTP `.Result`. The same service decisions, ProblemDetails, no-write/no-audit
rejection and all successful response shapes remain unchanged. No schema,
reference rule, permission, audit implementation or global result helper was
changed. Five real protected HTTP cases now pass, and the unchanged browser
assertion verifies both referenced value and category rejection without data
or audit changes. Red receipts remain under ignored artifacts as
codes-http-status-red.log and codes-category-status-browser-red.log/.zip;
green receipts are codes-http-status-green.log and codes-browser-*-focused.log.

## Backend-owned XLSX workflow

The frontend validates only the source file boundary: non-empty `.xlsx`, up to
10 MB. It buffers those bounded bytes once, requests dryRun=true, then submits
the identical file and captured category through fresh owned multipart streams
only after explicit confirmation. Central RBI transport owns private session
token propagation; tokens never enter browser JavaScript.

The actual backend parses the workbook, validates one column/header and up to
5000 rows, reports ready/existing/error counts, skips existing names, and retains
its partial-valid-row import behavior. Native UI permits the source's explicit
"Import valid rows" even with warnings, shows encoded candidate/duplicate values
and localized known row warnings, and reports the actual imported count. A real
fixture proves a generated-code collision imports1 of2 candidates, rather than
claiming all candidates were saved. Cancel/preview do not write or audit.
Corrupt/header-invalid files safely fail without exposing backend exception
text. No automatic mutation retries; unknown outcomes warn to refresh first.

## Evidence

- 28 new frontend API/draft cases plus signed-out route coverage.
- Five real HTTP regression cases for the existing backend result wrapper.
- Ten Admin browser cases: actual CRUD/audit, live business-form reference refresh, validation/duplicate rejection,
  referenced delete rejection, category history, real XLSX preview/partial import,
  safe file errors, outage recovery, accessibility, 375px bounds, focus and BS/EN.
- Denied route and six actual API write probes for each other DB role; backend
  read remains200. Workers=1/retries=0; no assertion/timeout weakening.
- Mobile screenshot visually inspected; scoped dialog/control border-box sizing
  preserves horizontal table scrolling without page/dialog overflow.
- Full migration .NET suite:193 frontend +210 backend unit +109 integration =
  **512/512 PASS**, no skips. Release publish and git diff --check pass. Changed
  new C# files were formatted without reformatting the existing controller.
- Focused browser gate:10 Admin +1 each for gost/Kapital/PL/Limiti/FL =15 PASS.
  The previous complete six-role gate was158/158 at the period checkpoint; that
  historical total is not represented as a new full rerun here.

Host receipts use real controllers, services, DB grant transformation and audit
over unique InMemory stores with test-only identity. No backend Program/.env,
live SQL, Keycloak, SMTP or real data is used. Existing Pkcs dependency advisories
remain unresolved. React and deployment stay unchanged. Guide/profile-wide and
integrated release gates are tracked separately in the parity matrix.

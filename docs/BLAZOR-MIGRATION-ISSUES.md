# Migration findings / release risks

## Platform file capability — resolved locally

The original finalized0.1.0 transport only exposed JSON and kept token session
access internal. The owner approved a narrow platform extension on2026-10-22.
Platform commit `a38b94c` on `codex/api-file-transport` adds
`IRbiApiFileTransport` (multipart POST / bounded binary GET) without changing
the existing JSON interface or authentication production code. Its full .NET
regression passed1291 tests. The app consumes packed ApiCore/Authentication
`0.1.1-migration.1`, not platform source references. Corporate publication and
deployment approval are not implied by local packing. The ten platform forensic
files remained byte-identical and untracked.

## RbiSelect boolean conversion — separate platform finding

A real browser run proved `RbiSelect<bool?>` calls framework BindConverter on a
string option value and throws InvalidCastException (string→nullable bool),
terminating the circuit. This was not a native form submit/viewport issue.
No DesignSystem fix is included in the approved ApiCore/Authentication scope.
Physical-person Yes/No choices use the existing typed `RbiRadioGroup<bool?>`,
which also matches the old application's segmented-choice behavior. Explicit
unselected/true/false values and family-dependent disabled choices are preserved.
String/integer/GUID select controls remain in use. Evidence is the original
browser trace and subsequent passing resident/nonresident/family workflows under
ignored `artifacts/migration-browser/`. Do not silently advertise boolean-select
support as fixed in the upstream library.

## Import error privacy — intentional UI adaptation

Existing import APIs return errors as strings; some exception paths can include
internal details. The native UI does not render those upstream strings. It
shows imported/failed counts, recognized column-count/header positions and known
localized row/field guidance while the
backend keeps its original validation and result contract. The original React
source is unchanged. This privacy requirement is not a reason to parse Excel
in the frontend, change persistence, suppress failed rows or claim all rows
were successfully imported. More precise structured safe backend error codes
may be introduced separately if required; do not display raw SQL/stack traces.

## Legal business validation HTTP status — locally corrected

The real LegalEntityService throws the root business ValidationException, which
the global handler previously classified as HTTP 500. Isolated HTTP regression
tests proved duplicate FBA and reversed dates failed as server errors (2 failed,
1 privacy test passed). The handler now returns HTTP 400 for this specific
exception and adds a field-keyed `errors` collection with generic safe guidance.
It does not expose raw business messages, interpolated identifiers or unexpected
exception details. Validation rules, persistence, schema and controllers are
unchanged. The unexpected-exception privacy test still returns a redacted 500.
Green evidence: `artifacts/migration-template/legal-validation-green.log` (3/3)
and `legal-validation-regression.log` (58 frontend + 210 unit + 67 integration =
335/335, no skips). These receipts use isolated InMemory/test identity, not SQL
or deployed SSO. Existing dependency advisories remain unsuppressed.

## Code-list HTTP error wrapping — locally corrected

Native browser integration found that rejected category deletion preserved data
but returned HTTP200. Four actual HTTP regressions also proved the same wrapper
issue for missing category and invalid/missing value updates. The narrow two-
branch controller fix returns the existing helper's HTTP Result rather than
serializing a typed ActionResult as an object. Five HTTP cases now pass, including
the already-correct invalid-read400 case. Reference rules, storage, permissions
and successful contracts are unchanged. See BLAZOR-MIGRATION-CODE-LISTS-CLOSURE.md
for red/green receipts and unchanged no-write/no-audit rejection assertions.

## Known dependency advisories — unresolved release risk

The preserved backend dependency graph resolves
System.Security.Cryptography.Pkcs9.0.4 with existing NU1902/NU1903 advisories
GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx. They are also reported by the
test-only host that references the real backend. No warning suppression or
unreviewed dependency upgrade is part of this frontend migration. Local test
success is not a security release clearance.

## External/integrated gates — not yet proven

Local HTTP/browser tests isolate identity and EF InMemory storage; they never
run backend Program/.env, SQL Server, production migrations/seeding, real SSO,
SMTP or OCP. The app has no production fake-identity mode. The approved shared
cache/provider, confidential client/audience configuration and deployed Keycloak,
SQL and OCP environments must be verified before production claims or cutover.
Local business modules are now closed in the parity matrix and final handoff;
the integrated infrastructure/security/cutover gates above remain open.

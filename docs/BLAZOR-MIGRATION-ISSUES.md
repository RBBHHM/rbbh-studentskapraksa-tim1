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
Other business modules remain tracked individually in the parity matrix.

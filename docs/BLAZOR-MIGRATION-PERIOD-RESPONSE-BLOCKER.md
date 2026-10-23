# Period clarification storage boundary blocker

2026-10-23. **BLOCKED — new data-contract decision required.** Native Users is
closed separately at 4566a50e9d4b609d90a0a8e3712d9d45bcc02f9f; its accepted
checkpoint has 450/450 .NET and 141/141 browser passes. The previously approved
users/admin safety blocker remains RESOLVED; this is a different existing flow.

## Confirmed contract mismatch

The current React PeriodPage permits clarification text of 10..500 characters.
`PeriodLockController.RespondToRequest` only validates the minimum, trims the
message, prefixes it with `[ODGOVOR] ` (10 characters), and creates a new PENDING
UnlockRequest through the real repository. The original NEEDS_INFO row remains.

UnlockRequest.Reason has StringLength(500). The current EF model/snapshot and
InitialSqlServer migration declare nvarchar(500). No later storage expansion was
found. Therefore a source-valid 491..500-character message becomes a
501..510-character value, outside the repository's declared SQL storage contract.
This is not solved by using InMemory, hiding text or truncating user content.

Real protected HTTP discovery uses the production controller, repositories,
AuditService, DB role transformation and actual Admin grant. Only identity, unique
InMemory state and email delivery are isolated fixtures. The starting request is
NEEDS_INFO, matching the source UI's respond action.

| Message length | Actual HTTP | New stored Reason length | EF maximum | Invariant |
|---|---|---|---|---|
| 490 | 200 | 500 | 500 | PASS |
| 491 | 200 | 501 | 500 | FAIL |
| 500 | 200 | 510 | 500 | FAIL |

The test compares the accepted value with actual EF metadata and verifies that
the full submitted text is retained. It permits safe 400/no-write rejection as
an alternative, without deciding the business policy. InMemory does not enforce
SQL column lengths; **no live SQL truncation exception or actual customer data
loss is claimed**. The proven problem is acceptance/construction/persistence of
a value inconsistent with the declared relational contract. SQL deployment
behavior still requires its own environment acceptance.

## Reproduction and working-tree state

Uncommitted discovery file:
`RelatedPartiesRegister/IntegrationTests/Tests/PeriodLock/UnlockResponseStorageDiscoveryTests.cs`.

Run the IntegrationTests project with `--artifacts-path artifacts/users-safety
--no-restore --filter FullyQualifiedName~UnlockResponseStorageDiscoveryTests`.
Ignored receipt: `artifacts/migration-template/period-response-storage-discovery.log`.
Result **2 FAIL / 1 PASS**, no skips. The test is deliberately not committed as
a passing production regression. A full run of the working tree including these
new cases must not be described as green; the 450/141 results are the accepted
Users checkpoint immediately before discovery.

The minimal Testing host does not execute backend Program/.env, SQL, Keycloak,
jobs or SMTP. Email is a strict mock limited to the response notification. No
production backend behavior, schema, migration, React source or real records was
changed in this discovery.

## Decision needed

Recommended no-schema-change option: explicitly limit the clarification itself
to **10..490 characters**, enforced server-side and in the native UI, retaining
the existing prefix and full accepted user text. This reduces the current UI's
500-character allowance and therefore requires business approval; it must not be
silently introduced as a migration detail.

Alternatively, retaining a full 500-character clarification requires an agreed
change in how its response marker is represented or an explicitly approved
storage/schema change. Do not silently trim content, remove the marker, expand
the database, fake a successful save or weaken the invariant test.

Period workflow migration pauses here pending that decision. Code-list/guide and
integrated deployment gates remain incomplete. No cutover or push is approved.

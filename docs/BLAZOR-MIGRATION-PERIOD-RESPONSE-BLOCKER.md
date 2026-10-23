# Period clarification storage boundary blocker

2026-10-23. **RESOLVED — explicitly approved 10..490-character response limit.** Native Users is
closed separately at 4566a50e9d4b609d90a0a8e3712d9d45bcc02f9f; its accepted
checkpoint has 450/450 .NET and 141/141 browser passes. The previously approved
users/admin safety blocker remains RESOLVED; this is a different existing flow.

## Historical contract mismatch before the fix

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

## Historical red reproduction

At discovery the uncommitted file was:
`RelatedPartiesRegister/IntegrationTests/Tests/PeriodLock/UnlockResponseStorageDiscoveryTests.cs`.

Run the IntegrationTests project with `--artifacts-path artifacts/users-safety
--no-restore --filter FullyQualifiedName~UnlockResponseStorageDiscoveryTests`.
Ignored receipt: `artifacts/migration-template/period-response-storage-discovery.log`.
Result at that checkpoint: **2 FAIL / 1 PASS**, no skips. It was deliberately not
committed as a passing production regression. The discovery has now been replaced
by UnlockResponseHttpTests with explicit accepted/rejected boundary assertions.
The 450/141 results describe the Users checkpoint immediately before discovery.

The minimal Testing host does not execute backend Program/.env, SQL, Keycloak,
jobs or SMTP. Email is a strict mock limited to the response notification. No
production backend behavior, schema, migration, React source or real records was
changed in this discovery.

## Approved correction and current evidence

The user explicitly approved **10..490 characters** for the clarification itself,
server-side and in the native UI, with the unchanged prefix and SQL schema. This
is a documented intentional reduction of the old React allowance, not accidental
parity drift. React/reference source remains unchanged.

The narrow backend guard and HTTP regressions are committed separately as
`cbe95276e424c698bb482713bb32542eee6ae3e3`
(`fix(period): bound unlock response to persisted length`). Native period UI
and module evidence follow in `474734e`.

The backend rejects raw input longer than490 and trimmed meaningful text shorter
than10 before lookup/write/audit/email. It returns safe HTTP400 with field
`message` and the business range, without echoing user text or internal details.
Existing whitespace normalization remains; there is no substring/truncation of
accepted content. The exact existing `[ODGOVOR] ` prefix remains, so the largest
accepted final Reason is500. No entity, migration or SQL schema was changed.

The actual native PeriodNoteForm sets minlength=10 and maxlength=490 for responses,
performs independent model validation and shows clear BS/EN range feedback. Its
character counter continues the existing React note-dialog pattern; it was not
added solely for presentation. Other request/admin-note purposes retain500.

- Focused period suite: **21/21 PASS**, including9 new real-HTTP cases and12
  existing period cases. Boundary9/491/500 =>400,10/490 =>200. Accepted records
  preserve exact prefix+text and fit the EF500 contract; rejected cases create no
  response/audit/email. Tests also cover meaningful whitespace minimum, actual
  NEEDS_INFO→new PENDING→unlock/approval state and audit, and forged-grant denial.
- Full backend **210 unit +104 integration =314/314 PASS**; including frontend,
  **478/478 .NET PASS**. No skips.
- Native browser cases independently confirm all five boundaries. Invalid UI
  values bypass only the HTML maxlength in the test to prove model validation;
  direct protected HTTP probes separately prove backend authority. No assertion,
  retry, timeout or production control was weakened.
- See BLAZOR-MIGRATION-PERIOD-CLOSURE.md for the whole native period flow/browser
  evidence, publish, safe unknown-outcome recovery and environment limits.

Green receipts: `artifacts/migration-template/period-bound-focused.log`,
`period-regression.log`, `period-browser-*.log`, `period-publish.log`. Builds use
the isolated artifacts/users-safety output, without stopping running user hosts.
Real SQL/SSO/SMTP were not exercised. Code-list/guide and integrated deployment
gates remain incomplete; no cutover or push is approved.

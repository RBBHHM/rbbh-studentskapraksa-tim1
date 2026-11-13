# Identity and global-period hardening — 2026-10-26

These owner decisions supersede the 18702db reconciliation. They are behavioral
and security changes, not a visual redesign. Claude's separate visual batch was
subsequently integrated; tracked React source was retired only after its gates.
The identity/global-period contracts below remain current.

## Actors and recipients

Human business controllers require authenticated ClaimTypes.Email before action
execution. Missing/blank email returns safe401 before service writes, audit or
mail. Name, preferred_username, sub, machine name and system are never fallbacks.
The authenticated email is used consistently for author/audit attribution.

EmailSettings no longer fabricates recipients/senders. Real SMTP/HTTP providers
validate required configuration on startup; HTTP sender rejects missing transport,
sender or recipient without recording a sent mail. Unlock request/respond reject
missing Email:AdminEmail with safe503 before storing. Explicit appsettings
recipient/sender addresses are configuration, never identities; owners must verify
them for the environment. Demo mail is a non-delivery simulator, not SMTP proof;
production delivery must select/configure a real provider. Synthetic claims
belong only to the gated development/test paths.
The scheduled AutoLock service has an explicit non-human "system" service actor;
it does not resolve a missing human identity and is never used by controllers.

The per-occurrence source audit is in IDENTITY-OCCURRENCE-INVENTORY-2026-10-26.csv.
TEST_FIXTURE includes negative examples in historical reports. DEVELOPMENT_ONLY
includes the retained React showcase examples and explicitly gated local seed/auth.
REAL_CONFIGURATION includes explicit recipient/sender settings, real claim
consumers and the named scheduler actor. PRODUCTION_BUG entries identify the
removed actor fallbacks at the original HEAD; no such fallback remains active.

## One global period

Every protected route evaluates period(year,month), never a username prefix or
department. DepartmentHelper is deleted. Repository/controller/client contracts
carry no department input or output; obsolete query parameters have no effect.
The historical SQL Department column is retained without a destructive migration.

Canonical row: Department IS NULL. With no row, prior periods remain locked;
a historical non-null Department row also fails closed. New writes do not silently
replace historical rows. Canonical global state cannot be overridden per department.
Before deployment, DBA inventory:

```sql
SELECT Id, Year, Month, Department, IsLocked, LockedBy, LockedAt
FROM PeriodLocks WHERE Department IS NOT NULL ORDER BY Year, Month;
SELECT Year, Month, COUNT(*) AS RowsPerPeriod FROM PeriodLocks
GROUP BY Year, Month HAVING COUNT(*) > 1;
```

Export and retain historical/audit data. An authorized business owner decides ONE
global state per period. If a single row exists, review changing Department to NULL
inside a controlled transaction and record the decision/audit; do not delete rows
or infer global unlock from a department. If duplicates/conflicts exist, resolve
them explicitly against the deployed schema/indexes before application writes.
No production reconciliation was executed here.

Physical/legal create/edit/deactivate/import/family/verify, limits create/edit/delete
and capital save all fail closed in native UI when locked or state is unavailable.
Reads/search/export remain. A form opened before the lock still gets backend423;
it keeps its draft, displays a safe error, disables submit and does not retry/write.

## Virtual Guest and DB roles

Only FL, PL, limiti, kapital and admin are assignable. gost is an EFFECTIVE default,
centrally defined in RolePermissionMatrix, not a required stored role/mapping.
All authenticated identities without a confirmed explicit mapping receive Guest
reads, including identities without any AppUser row. Provider Admin claims are
discarded. Explicit DB grants remain authoritative and are recomputed per request.
Disabled matching subject/email records deny access and cannot fall back to Guest.

Clearing roles retains the AppUser, IsActive and subject/lifecycle metadata.
An active zero-role user is Guest; inactive users have no grants. Self changes
and final-active-Admin protections remain. Old gost rows/mappings are tolerated
but ignored for assignment; their removal is an approved data-cleanup step, not a
runtime dependency. No role reseed, broad deletion or realm-role rewrite occurs.
An unconfirmed Admin placeholder does not count as a remaining usable Admin:
the continuity guard counts active confirmed Admin subjects only.
Reports read/download follows Guest; generate/persist still requires KAPITAL_CREATE.
The profile additionally returns the recomputed applicationUserId for own-row UX.

## Durable subject model

KeycloakId historically included generated GUID placeholders when Keycloak admin
integration was disabled. An identifier is NOT trusted merely because it looks
like a GUID or equals sub.

Migration 20261026115804_ConfirmKeycloakSubjects adds
KeycloakSubjectConfirmed (default false) and KeycloakSubjectLinkedAt (nullable).
It does not confirm/copy/update historical values. The existing unique KeycloakId
index remains. New users resolved through actual Keycloak Admin lookup may be
confirmed; local placeholders remain unconfirmed. Explicit development seed
identities are confirmed only within the existing development-only seed path.

Runtime: confirmed sub first; inactive subject denies; never replace a confirmed
sub by email. A unique normalized active email can link only once, with actual
email_verified=true, nonempty bounded sub and explicit
IdentityBinding:AllowVerifiedEmailFirstLink=true. Default is false. Unverified,
ambiguous or already-bound-different-sub identities cannot inherit explicit roles;
they retain virtual Guest only unless a disabled match requires denial. Binding
rechecks candidates under the same transaction-owned SQL application lock used by
user safety, saves actual sub/confirmation/time and SUBJECT_LINK audit atomically.
Future email changes cannot rebind a confirmed subject.

## Production schema and one-time backfill — NOT executed

F3 remains **PARTIAL: CODE/SCHEMA READY; PRODUCTION DATA RECONCILIATION OUTSTANDING**. No real
realm export or user data was read. Never mark every existing GUID confirmed.

1. Verify real realm issuer/audience/sub and that email_verified is emitted and
   represents verified mail. Export the approved realm users and application
   identity rows securely. Review unique normalized email, collisions, existing
   placeholders, inactive accounts and intended application roles.
2. Generate/review the additive migration with the approved EF tool/feed:
   `dotnet ef migrations script 20260929102823_AddGlobalCapital 20261026115804_ConfirmKeycloakSubjects --idempotent --project Backend/RelatedPartiesRegister.csproj --output artifacts/subject-schema.sql`.
   DBA applies the reviewed SQL via the existing production migration policy.
   Startup never migrates/seeds. Back up first.
3. Prefer a signed-off per-row backfill: bind the parameters below from that
   approved reconciliation, including inactive records without activating them.
   Execute with a real operator email and audit in the same transaction.
   Never derive the sub from a local ID or guessed username.
4. Alternatively, after review enable the controlled first-link window for the
   approved active unique-email population, verify actual logins/audits, then turn
   it off. Outstanding unconfirmed rows receive Guest only, never their old
   privileged grants. Do not silently enable this window in appsettings.
5. Validate subject-first logins, changed email, disabled denial, zero-role Guest,
   Admin continuity and audit in UAT before cutover. Keep the signed-off mapping
   and rollback plan. Schema downgrade would remove confirmation metadata;
   it is not a harmless rollback after subjects have been linked.

Parameterized one-row DBA command (parameters supplied by approved tooling,
not literal sample identities; all string inputs must be bound without truncation):

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock
  @Resource=N'RBBH.Users.ActiveAdminSafety', @LockMode=N'Exclusive',
  @LockOwner=N'Transaction', @LockTimeout=5000;
IF @lockResult < 0 THROW 51000, 'Identity reconciliation is busy.', 1;
IF @Subject IS NULL OR DATALENGTH(@Subject) NOT BETWEEN 2 AND 200
  OR @OperatorEmail IS NULL OR DATALENGTH(@OperatorEmail) NOT BETWEEN 2 AND 200
  THROW 51000, 'Invalid approved identity parameters.', 1;
IF (SELECT COUNT(*) FROM AppUsers WITH (UPDLOCK,HOLDLOCK)
    WHERE LOWER(LTRIM(RTRIM(Email)))=@NormalizedEmail) <> 1
  THROW 51000, 'Email reconciliation is ambiguous.', 1;
IF EXISTS (SELECT 1 FROM AppUsers WITH (UPDLOCK,HOLDLOCK)
           WHERE KeycloakId=@Subject AND Id<>@AppUserId)
  THROW 51000, 'Subject is already linked.', 1;
UPDATE AppUsers
SET KeycloakId=@Subject, KeycloakSubjectConfirmed=1,
    KeycloakSubjectLinkedAt=SYSUTCDATETIME(), ModifiedAt=SYSUTCDATETIME(),
    ModifiedBy=@OperatorEmail
WHERE Id=@AppUserId AND KeycloakId=@ExpectedLegacyId
  AND KeycloakSubjectConfirmed=0 AND IsActive=@ExpectedIsActive
  AND LOWER(LTRIM(RTRIM(Email)))=@NormalizedEmail;
IF @@ROWCOUNT <> 1 THROW 51000, 'Approved identity precondition changed.', 1;
INSERT AuditLogs (Id,TableName,RecordId,Action,OldValues,NewValues,UserId,Username,Timestamp)
VALUES (NEWID(),N'AppUser',CONVERT(nvarchar(200),@AppUserId),N'SUBJECT_LINK',
  (SELECT @ExpectedLegacyId AS legacyIdentifier FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
  (SELECT @Subject AS confirmedSubject FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
  @OperatorEmail,@OperatorEmail,SYSUTCDATETIME());
COMMIT;
```

DBA must verify table/schema naming and bind @AppUserId, @ExpectedLegacyId,
@ExpectedIsActive, @Subject, @NormalizedEmail, @OperatorEmail from the reviewed
manifest. Any exception requires rollback/disposal of that command transaction.
This is an operator runbook, not an automatically executed migration helper.

## Evidence and remaining gates

New tests exercise missing actor/recipient before HTTP mutation, username-prefix
independence, global current/status contract, historical-row fail-closed behavior,
virtual Guest without stored roles, disabled denial, confirmed-sub precedence,
verified controlled first-link and races. A disposable SQL Server test upgrades
the previous migration, proves placeholders unchanged, then confirms durable
first-link and audit via a fresh context. Last-Admin SQL HTTP concurrency remains
covered by all five existing SQL tests. Exact final totals/build/audit receipts
are recorded in REACT-TO-BLAZOR-FINAL-HANDOFF.md after the final run.

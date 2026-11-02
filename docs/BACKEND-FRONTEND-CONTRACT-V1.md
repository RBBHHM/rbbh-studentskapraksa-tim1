# Backend → Claude frontend contract, v1

Backend branch: `feat/codex-backend-finalization`; frontend owner: `feat/claude-frontend-finalization`.
Approved direction: user instruction dated2026-11-02. This contract freezes additive wire shapes before implementation. Sections identify availability; do not assume a planned endpoint is already deployed. No frontend file is changed by backend work. Backend does not duplicate Claude's BrowserHost assembly fix.

## Implemented security deltas

- Existing `POST /api/verification/{legal|physical}-person/{id}` retains its permission/period checks and success DTO.403 `verification.self_denied` for creator,409 `verification.provenance_required` for historical rows without proven creator,409 `request.concurrent_change` on a race. Creator is established server-side from authenticated subject. Update.Status cannot transition legal verification state (400 `verification.status_owned`); physical Update cannot bind status/creator fields.
- `GET /api/period-lock/unlock-requests` now lists only the current subject's records. Existing envelope `{requests,total,page,pageSize}` retained. Processing queue is additive `GET /api/period-lock/unlock-requests/review`, requiring existing CapitalWrite permission. Capital processing permission is preserved, not changed to Admin-only. Point the processor table to `/review`; user views keep the original route.
- Responses use existing `POST /api/period-lock/unlock-requests/{id}/respond` with `{message}` (10–490 characters). Cross-owner404; only NEEDS_INFO may receive one response; repeat409. Existing original-state and new PENDING child workflow retained. Subject is never accepted from the body. Legacy ownerless records require approved provenance reconciliation.
- Raw `/api/email-log` HTML previews are Development/demo-only and restricted to matching recipient; production returns no raw stored messages. Use the recipient-scoped inbox below for the application.
- Error responses retain ProblemDetails or existing errors[] compatibility. New stable `code` extensions are safe to localize; do not parse exception messages. Correlation response header is additive. S1 requires `KeycloakSettings__Audience` outside Development; OIDC client identity is separate.

## C1: role requests and inbox (implementation follows this contract)

All routes require authentication. Request/owner identity comes from validated subject; verified email required to submit. Application database grants remain authoritative; unknown authenticated users retain virtual Guest without AppUser provisioning. No bearer token enters browser code.

| Method/route | Input | Success | Permission |
|---|---|---|---|
| GET `/api/role-requests/options` | none | `{items:[{id,name}]}` active requestable DB roles | authenticated |
| POST `/api/role-requests` | `{clientRequestId:guid,purpose:"new"|"change",roleIds:guid[],note?:string}` |201 request; matching clientRequestId replay200 same request | authenticated, verified email/subject |
| GET `/api/role-requests/mine` | page=1,pageSize=20,status? | `{items,total,page,pageSize}` current subject only | authenticated |
| GET `/api/role-requests` | same filters | same envelope | active application Admin |
| POST `/api/role-requests/{id}/approve` | `{decisionId:guid,expectedVersion:int,note?:string}` |200 final request | active application Admin; no self-approval |
| POST `/api/role-requests/{id}/reject` | same |200 final request | active application Admin; no self-rejection |
| GET `/api/notifications` | page,pageSize,unreadOnly? | `{items,total,page,pageSize,unreadCount}` | recipient subject only |
| POST `/api/notifications/{id}/read` | none |204, idempotent | recipient subject only; other owner404 |

Request DTO: `{id,purpose,status,roleIds,roleNames,note,createdAt,processedAt,processedBy,decisionNote,version}`. Administrative lists additionally include requester display fields; identity/approval fields are never writable. Status is `pending`, `approved` or `rejected`; these are workflow invariants, not configurable business dropdown values.1–20 distinct role IDs for new requests; change may remove all explicit roles subject to final-Admin protection. Note max1000. `new` unions approved requested roles with current roles; `change` replaces explicit roles. Empty explicit roles leave virtual Guest. Identical decisionId replay returns the existing final decision without repeated grants/audit/notice; incompatible/stale replay409. There is at most one pending request per subject. Cross-owner detail access never exposes identity/request content.

Notification DTO: `{id,titleKey,bodyKey,parameters,route,createdAt,readAt}`. Keys and parameters support existing RBI localization consumption; routes are authenticated relative app paths. Backend commits business state/audit/inbox/outbox together. Email delivery is asynchronous and may be retried without reapplying grants; exact-once email across provider/network crashes is not claimed without provider idempotency support.

## C2: backend session

Backend application cookie lifetime is an absolute8h maximum, with sliding expiration disabled. OIDC login remains authorization-code+PKCE; token issuer/signature/audience/expiry/realm revocation must remain valid. Refresh cannot extend the application session beyond8h. Session expiry returns401; logout clears the app cookie and follows the configured OIDC sign-out. Frontend RBI server-side session/token cache must adopt the same absolute cap; real realm refresh/SSO policy may require earlier reauthentication. End-to-end8h completion remains unverified until Claude's browser handling and real Keycloak are tested.

## C3: registry and workbook additions (pending backend implementation)

Existing legal create/update/response gain optional `gccNumber` and read-only resolved `gccName`, plus `isLargeExposure` boolean defaultfalse for new records. Historical values are not inferred. Missing authoritative bank lookup is a503 source-unavailable result, not fabricated data. Existing tax exact13+checksum remains until business confirmation. Existing clients may omit new fields during rollout; frontend should require GCC only after bank source configuration/readiness is confirmed.

Add paged authenticated `GET /api/bank-reference/customers?search=&page=1&pageSize=20` and `/api/bank-reference/gcc` (same query). Customer items identify taxNumber,matbroj,name; GCC items identify number,name. Existing local legal-limit lookup remains supported; additive paged local lookup uses the same envelope. Bank source needs approved `kod.dbo.komitent` and `glava.dbo.grupekom` mappings/connectivity; no credentials are supplied by code.

Excel exports retain business columns and add created/modified user/date metadata. Registry imports accept exported v2 columns and legacy templates/aliases. Metadata is read-only and ignored on import; creator/audit always comes from server identity. Partial success means all valid rows are staged and committed once, invalid rows are counted with row/field diagnostics, and a storage failure commits none. Keep `{imported,failed,errors}`; do not assume HTTP200 means every row imported.10MB compressed upload/5000 data rows stay bounded; expansion limits apply before ClosedXML loads content.

## C4: capital (pending backend implementation)

Existing `GET/PUT /api/capital` DTO retained. PUT creates an immutable effective-date revision instead of rewriting history. Add `GET /api/capital/history?page=&pageSize=` and `GET /api/capital/as-of?date=yyyy-MM-dd`. History adds createdBy/createdAt and revision identity. Same-date corrections append a revision; as-of chooses latest recorded revision for the latest effective date≤requested date. Older values are never reconstructed from today's mutable row. Current read uses today's as-of value; existing legacy row remains a provenance-preserved fallback until it is superseded by a dated revision. Report snapshots stay persisted. Capital edit permission and period lock remain mandatory. Finance exposures still require missing approved mappings/spreadsheets/reconciliation inputs.

## C5: localization/codebooks (pending backend implementation)

Reuse the standard application codebook area and RBI localization consumption. Two logical catalogs: languages and language packs; names configurable, no DP_ prefix requirement. Required initial supported codes BS/EN/DE. Language-pack record: active,languageCode,resourceKey,label,helper,placeholder; new keys lowercase semantic dot notation. No second frontend localization engine.

Add authenticated `GET /api/localization/languages`→`{items:[{code,name}]}` and `GET /api/localization/resources/{language}`→`{language,fallbackLanguage,version,items:[{resourceKey,label,helper,placeholder}]}`. Administrative writes use `PUT /api/localization/resources/{language}/{resourceKey}` with `{label,helper?,placeholder?,isActive}`. Runtime edits invalidate caches; fallback requested language→configured default→stable key, with bounded missing-key diagnostics. API errors use stable keys and safe values. Generic codebook import/export keeps existing field conventions and active/reference protections.

## Rollout and open external inputs

Apply reviewed additive migrations before upgraded API deployment. No automatic production DDL; preserve backups and rehearse a populated disposable copy. Creator/request-owner provenance columns remain null for historical rows; no inferred email-based backfill. Subject-link production migration requires real realm export and approval. SQL uniqueness additions require duplicate/orphan preflight. No UAT/PROD data migration is executed locally.

External owners must supply bank/finance mappings and samples, real Keycloak verified-email/audience/redirect/refresh behavior, mail delivery configuration, approved OCP/3scale/proxy/SQL/TLS values, shared sessions/DP keys, drain/scheduler/backup/retention operations. These do not block independent backend implementation, but they block production verification.

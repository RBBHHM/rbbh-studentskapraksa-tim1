# Final local migration security pass — 2026-10-23

Local remediation/regression evidence only. This is not production security,
SSO, SQL or OCP approval. Historical closure receipts retain their original totals.

## PKCS dependency — locally resolved

The actual isolated restore graph was:
`Microsoft.Data.SqlClient/6.1.3 -> System.Security.Cryptography.Pkcs >=9.0.4`,
resolving9.0.4. No application Pkcs reference existed. It produced NU1902/NU1903:

- [GHSA-2fmv-rv2c-59x7 / CVE-2026-70162](https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7): Moderate, Windows EnvelopedCms.Decode CPU exhaustion.
- [GHSA-846f-f764-76mx / CVE-2026-84008](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx): High, SignedCms.CheckSignature CPU exhaustion.

Both advisories identify9.0.21 as the patched9.x version. A direct backend
PackageReference pins [Pkcs9.0.21](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/9.0.21),
satisfying SqlClient's minimum on the same dependency line and target framework.
No SqlClient/EF/TFM upgrade or NU1902/NU1903 suppression was introduced.
Fresh restore resolves9.0.21; full solution analyzer build has0 warnings/errors.
This fixes these package advisories, not every possible deployed runtime issue.
Production base/runtime servicing and corporate dependency approval remain gates.

## Authentication metadata TLS — locally resolved

Both backend JWT and OIDC options formerly disabled RequireHttpsMetadata for any
issuer starting with `http://localhost`, in any environment. Six option-pipeline
cases proved4 failures: Production/Staging/UAT localhost HTTP and a Development
`localhost.attacker.test` lookalike accepted insecure metadata. The narrow fix
allows HTTP metadata only when Development AND Uri.IsLoopback are true.
JWT/OIDC framework post-configuration now rejects all four unsafe cases before
network access; genuine Development loopback and HTTPS behavior remain covered.
No role, identity-link, token-validation or permission-store redesign occurred.

## Authorization and application safety

The additional six-role browser matrix calls actual protected controllers through
the isolated backend HTTP host, including forged provider Admin claims. Wrong
module writes must return403; authorized invalid probes reach400/404 business
validation without writes/audit. Reports/current-period/codebook reads remain
available; users/audit reads remain Admin-only. Existing positive module workflows
separately verify successful writes and audit, not just rejected probes.

Existing real-HTTP reporting12 cases, users safety28 cases and period21 focused
cases remain in the full regression and pass the fresh61/61 focused run
(completion-focused-safety.log). Identity is Keycloak; application grants
remain database-owned. Self-deactivate/delete and final-active-Admin loss remain
server-rejected. Period responses retain10..490 plus unchanged prefix<=500.

## Source review and limitations

Tracked-source review covered Password/ClientSecret/token/config logging, CORS,
Development fallback, exception-to-UI handling, frontend DB/Admin API boundaries
and current DB grant transformation. Credential-marker scans found no private-key,
GitHub-token or AWS-access-key markers. This is a bounded source review, not a
guarantee of zero secrets or a substitute for organizational secret scanning.
No .env, user-secrets or real credentials were read or printed. Synthetic fixture
strings are not production credentials. Backend startup does not log DB passwords;
there is no wildcard CORS grant or native frontend direct DB/Keycloak Admin call.
Native error presentation uses app-owned safe message keys, not raw exceptions.

Do not mistake remaining environment assumptions for local proof:

- Backend forwarded-header trust assumes the controlled OCP ingress network;
  actual ingress isolation/proxy trust needs deployment acceptance.
- Existing manual OCP workflow uses insecure certificate verification and UAT
  Development environment selection. Do not copy these into a newly approved
  native production rollout; CA/TLS and environment mapping need owner acceptance.
  No existing deployment was executed or silently replaced.
- Real issuer/audience/login/logout/refresh, SQL app-lock/constraints, SMTP,
  approved shared cache/DataProtection, secret injection and multi-pod circuits
  remain unverified outside isolated local fixtures.

Receipts under ignored `artifacts/migration-template/`: completion-restore.log,
completion-metadata-red.log (4 FAIL/2 PASS), completion-regression.log (528 PASS),
completion-build.log (0 warnings/errors), completion-format.log and
completion-matrix-{admin,gost,kapital,PL,limiti,FL}.log (1 PASS each).
Full browser receipts and final totals are recorded in the final handoff.

## Subsequent Claude reconciliation — 2026-10-26

The final handoff records six cherry-picked security commits and their fresh
regression. They supersede the original authentication-only reporting read
boundary: unknown/inactive identity is not Guest. JWT Audience fails closed
outside Development; self-role management is denied; Capital and verification
are period-locked. Native UX/contract tests now follow those backend rules.
Five actual SQL Server LocalDB last-Admin HTTP/concurrency cases pass on disposable
databases; deployed SQL/multi-replica acceptance remains external.

F3 remains PARTIAL: subject-first/verified unique normalized-email lookup does
not permanently persist a fallback sub. Approved one-time subject-link backfill,
actual email_verified and real Keycloak export/data reconciliation are still
required. No production data migration was performed.

Pkcs remains9.0.21. The earlier local patched-version result does not certify
today's production vulnerability feed, base image or private-feed availability.
Production vulnerability-feed confirmation is external; NU1900/NU1902/NU1903
are not suppressed or reclassified as passes.

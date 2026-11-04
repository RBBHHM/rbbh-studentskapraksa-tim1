# Migrating the default branch to the clean candidate

## Why `main` cannot be merged or rebased
`main` (`48cf8da`) contains three commits after the common ancestor `db77d3e`. The first of them (`6fb7605`) commits a working Keycloak client credential in `Frontend/appsettings.json`. Any merge or rebase of this lineage onto `main` would make that commit reachable from the candidate. This branch descends from `db77d3e` (before the credential) and from Codex's sanitized backend, so it does not contain the value in any reachable commit tree or message (verified, see below).

PR #4 shows conflicts because both lines changed the same authentication files after `db77d3e`. That is expected and is **not** resolved by a Git merge; each file was decided on its merits.

## Decision per conflicting file
| File | What `main` changed | Decision |
|---|---|---|
| `Backend/IoC/Extensions/Authentication/AuthenticationServiceExtensions.cs` | Removed the audience setting and its fail-closed check; set `ValidateAudience = false`; added `Console.WriteLine` of subject, username and audiences in JWT events (duplicated twice) | **Keep integration.** Audience is mandatory outside Development, token details are not logged. Only a harmless `MapInboundClaims` reordering existed on `main`, already equivalent here |
| `Backend/Program.cs` | Per-request console log of subject, application user id, roles and permissions; unconditional `MigrateAsync()` in Development | **Keep integration.** No identity logging; migrations only when `Database:ApplyMigrations` is explicitly true in Development |
| `Backend/BL/Services/KeycloakAdminService.cs` | Admin client id read from `KeycloakSettings:ClientId` | **Keep integration.** It must read `AdminClientId`; using the sign-in client id mixes two clients (backend finding ID-04) |
| `docs/PRODUCTION-READINESS.md` | Dropped `KeycloakSettings__Audience` from required startup variables | **Keep the consolidated state.** The file was folded into `README.md` and `docs/ONBOARDING.md` (which state that audience is required); the `main` edit would have re-introduced the weaker claim |
| `docs/configuration/ENVIRONMENT-VARIABLES.md` | Dropped the audience row and added a stray note to the admin client row | **Keep the consolidated state.** Same reason; the `main` edit also added an inaccurate fragment |

## Other `main`-only changes (not conflicts) and what was done
| Change on `main` | Decision |
|---|---|
| `Frontend/appsettings.json` with a real authority, client id and **secret** | Not carried. Configuration values belong to environment variables or user-secrets. **Rotate the secret.** |
| `Backend/appsettings.json`: `ApplyMigrations: true`, audience value removed | Not carried (production-unsafe default) |
| `AssemblyName` `DATA_CORE` in both web projects | Not carried; two projects with the same assembly name break the test host. The backend keeps the opt-in `UseLocalDataCoreAssemblyName` switch |
| `Frontend/Platform/RBI.Authentication/IoC/Authentication.cs`: `PushedAuthorizationBehavior.Disable` | **Owner decision.** Not carried because it is a platform change with no test here. If login against the identity provider fails with a pushed-authorization error, this one line is the likely fix; add it behind a setting and test it |
| `launchSettings.json`, `.csproj.user` (local debug profiles) | Not carried (developer-local) |
| `[Related Parties Register] Dokumentacija.pdf` | **Retained in `main`** at the owner's request (the four mapping workbooks and the `.msg` mail file were already part of the candidate). Note the PDF is a requester document: keep the repository private or restrict access |
| `docs/POPRAVKE.txt` | Already present on this branch |

## Evidence
* Reachable commits scanned: 168 on the candidate; the credential occurs in 0 trees and 0 messages. On `main` it occurs in 3 commit trees.
* `scripts/security/scan_git_secrets.py`: 0 findings on this branch; 1 finding on `main`.
* Source tree: identical to `integration/full-stack-final` (`be2e735`); this branch adds only this file. Test results of `be2e735` therefore apply (see `docs/FINAL-REQUIREMENTS-COVERAGE.md`).

## Owner actions (not performed here)
1. **Rotate or revoke the exposed Keycloak client secret** in the identity provider. Rewriting Git history does not make an already-public secret safe. Treat the value as compromised until rotated, and check provider logs for use.
2. Decide on `PushedAuthorizationBehavior.Disable` (above).
3. Decide where the PDF, workbooks and mail file should live.
4. Switch the default branch (below).

## Recommended procedure to switch the default branch
Nothing here was executed. It needs repository-admin rights.
1. Rotate the secret first.
2. Review and approve this candidate branch (`release/clean-main-candidate`). Keep `main` and every existing branch untouched for audit.
3. Preferred, no history rewrite: create a new repository (or rename the current one to `...-archive` and make it private) and push the candidate as `main` of the new/renamed repository. Re-create branch protection and CI settings.
4. Alternative inside the same repository: Settings, Branches, change the default branch to `release/clean-main-candidate` (or a branch named `main-clean`), then protect it. The old `main` still exposes the secret to anyone who can read the repository, so make the repository private or archive the old branch, and ask GitHub Support to purge cached views and PR refs if the repository was public.
5. Close PR #3 and PR #4 after the switch; do not merge them.
6. Never force-push over `main` without a recorded decision: forks and clones keep the old history anyway.

## Remaining production blockers
Real Keycloak realm/audience configuration and the 8-hour session, bank reference and Finrep sources, a mail provider, shared session/data-protection keys on the platform, a deployed-API plus SQL browser smoke test, release-notes route, and the gaps listed in `docs/FINAL-REQUIREMENTS-COVERAGE.md`.

## Final state of `main` after the update PR
`main` keeps its full history (including the commit that exposed the credential, which is therefore still reachable from `main` and from this PR branch). The PR replaces the **content** of the tree with the tested candidate: application source, configuration, tests, migrations and the consolidated documentation. Legacy documents folded into `README.md`/`docs/ONBOARDING.md` and root helper files (`env-variables.md`, `schema-overview.md`, `Frontend/README.md`, the `docs/*` legacy set) are removed from the tree and remain available in history. Because history is preserved, **rotating the credential remains mandatory**.

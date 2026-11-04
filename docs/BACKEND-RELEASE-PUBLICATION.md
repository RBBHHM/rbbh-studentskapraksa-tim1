# Sanitized backend publication and security gate

This review branch is `codex/backend-finalization-sanitized`, prepared from original backend `feat/codex-backend-finalization` at `0cf596d5d02739479e70f5ac7e700c417ca2f0dc`. Original local refs, shared history and Claude's worktree are unchanged. No force-push, merge or production credential rotation is authorized/performed by this preparation.

## Why a separate history

The public repository is RBBHHM/rbbh-studentskapraksa-tim1, default/only remote branch main at `48cf8da8f0bb77129ea0b14ad5366c7cea7ce14b` at inspection. Original backend has 134 ancestors. The known frontend client credential is present in 43 commit trees, including **all 40 newly publishable local commits**. Removing it only at the tip would not satisfy publication safety. The first affected commit is 6fb7605744b2b16362ea0f41a929ced053177ca1.

Clean common base: `db77d3e0231b5c0685612e8008fdcbfe2e4b4e99`, before that credential and requirements PDF were introduced.43 descendant commits were recreated on that existing base with empty Frontend/appsettings.json Authentication:ClientSecret and without the requirements PDF. Author/committer names, emails and dates are preserved; original messages retained with Sanitized-From provenance footer. The mapping below preserves review attribution. Existing shared refs/history were not rewritten.

The source snapshot after reconstruction is identical to the original backend/test trees and docs/POPRAVKE.txt. Frontend UI/components, DTOs, APIs, SQL migrations, permissions and business rules were not changed. The only original-snapshot content differences are the cleared client-secret value and omitted source PDF. Subsequent publication commits add scanning/CI/documentation only. All mutable contact or token values must come from environment/approved secret stores or local user-secrets; .env/user-secret files remain ignored. No token/password is requested in chat or committed as a deployment example.

## Publication scanning

Run `python scripts/security/scan_git_secrets.py --ref HEAD` and `python -m unittest discover -s scripts/security -p 'test_*.py'`. The guard scans every reachable blob and commit message using Git object plumbing, reports only rule/key/location/object ID, and fails closed on inspection errors. It checks keyed literals, environment/YAML credential scalars, connection passwords, provider-token/private-key patterns. Empty values, explicit external placeholders/references and narrowly scoped synthetic test literals are permitted. One historical pure connection-string unit fixture has a value-digest exception scoped to its exact test file; another path/value remains rejected. Tests cover secret-in-ancestor despite clean tip, output redaction and false-positive syntax boundaries. It is a defined static guard, not proof against arbitrary encoded/computed/encrypted secrets.

The image workflow now runs this guard and its 12 regression tests before build/browser jobs, with full checkout history. A PR scans the **source-head SHA**, not GitHub's synthetic merge commit: target main still contains historical exposed credentials. Source-head success does not certify the contaminated target or authorize a merge. Existing source/frontend CI assertions are not weakened; the known frontend DATA_CORE collision can still fail full-solution/browser jobs on this backend-only branch.

The original 134-commit history was inspected without emitting credential values. The known credential maps to one unique blob; all source/frontend config candidates and fixture references were reviewed. The sanitized branch has no known-credential bytes reachable and no guard findings. Original artifacts/maps and redacted scan outputs are local under ignored artifacts/backend-release*. They are not uploaded as logs containing secrets.

## Rotation and existing exposure

**The existing public main/history still exposes the old credential.** Sanitizing a new source branch does not revoke it, remove old remote refs/caches/clones, certify current token validity or repair shared history. Realm/security owner must rotate/revoke the exposed Keycloak credential, update every approved deployment/secret-store reference and confirm old client-credentials authentication fails. Check audit/use logs and notify repository/security owners through the authorized process. No rotation or shared-history rewrite was attempted here. Production promotion remains blocked until confirmation.

## Requirements and authorized reviewer access

The supplied `docs/POPRAVKE.txt` is retained byte-for-byte as explicitly requested; it is not reconstructed from memory. Backend finalization, exact final API contract, independent audit, integration notes and integration handoff remain present. Historical checkpoint references in these documents refer to their original source commits, not new sanitized hashes.

The 20-page PDF **[Related Parties Register] Dokumentacija.pdf** is intentionally not redistributed in this branch: no public redistribution permission was supplied. Its exact local source remains:

`C:/Users/mahmu/Desktop/rbbh studentske prakse/studentskapraksaTema1-codex-backend-finalization/[Related Parties Register] Dokumentacija.pdf`

Identification: supplied author Hana Mahmutović, V1, dated 30.10.2026; SHA-256 `be69033f8a14e8fab3c1a550f454d12e56646c80721970f14061bcfed2c73286`. An authorized reviewer can obtain that exact file from the project/requirements owner through an RBBH-approved channel or access the preserved original checkout. The source file was not deleted. Its prior presence in public main does not establish redistribution permission; repository owner must also review that existing exposure. No confidential PDF pages/images/extraction are added as substitute artifacts.

## Verification and integration disposition

Fresh sanitized-checkout regression: 337 unit passed, 395 integration passed, 0 failed/skipped; combined732/732. SQL-classified 86PASS (subset), including 6last-Admin and 3 audit-boundary regressions. Publication guard 12PASS, backend analyzer build 0 warnings/errors. Release publish PASS. Published Development/InMemory smoke at 5374: live/ready/profile/legal/report lists 200, Guest audit/metrics/legal-write/report-generate 403, unconfigured GCC 503. Owned smoke process was stopped; other hosts unchanged. Existing EF required-navigation warning is a runtime notice, distinct from compiler 0 warnings. No realSSO/source or production SQL startup is inferred. PKCS9.0.21 remains; no NU1900/NU1902/NU1903 suppression. No unsupported realSSO/bank/finance production success is inferred.

Claude's separate integration branch was observed at 22f57e9615811231b232c6a4275a198afbac986c. It already includes original backend contract HEAD 22f0376; original final audit HEAD 0cf596d was not present in that repository at inspection. Therefore the publication PR is **draft/informational, not a request to duplicate-merge this rewritten lineage**. The owner should integrate only outstanding approved changes, including original audit fix bddc913 (sanitized counterpart in mapping) and publication guard/config cleanup as needed, preserving Claude's existing frontend fix and secret removal. Do not overwrite his branch or merge a parallel copy of the whole backend. Default remote main is the available informational review target; no merge is requested.

Remaining production/integration issues are in BACKEND-INDEPENDENT-AUDIT.md and BACKEND-INTEGRATION-NOTES.md: coordinated cookie/CSRF/logout policy, expensive-operation admission/tree/export bounds, unexplained paging latency, legacy migration baseline/backup, identity backfill and real realm/bank/finance/mail/cache/DP/proxy/resources/drain/calendar/feed validation. No speculative API or authorization change was introduced to make a release gate appear green.

## Source-to-sanitized commit mapping

| Original source SHA | Sanitized SHA | Original author | Subject |
| --- | --- | --- | --- |
| `6fb7605744b2b16362ea0f41a929ced053177ca1` | `e923f0ff27a5b640470d634392590bf36b4c9056` | HANA MAHMUTOVIC | new |
| `63b0b673a1b147bc647fea5732616dd94a808b90` | `3f80cf0f743148dcf453f970eb6ab0c6dab2a770` | HANA MAHMUTOVIC | new |
| `48cf8da8f0bb77129ea0b14ad5366c7cea7ce14b` | `26284bc18fd12608fd65102633b20cfb33a65b8a` | HANA MAHMUTOVIC | new |
| `43670616b2a46a7b91310d09f02222f8187094f0` | `6a23420a14b75d23e735728569f5fc9294f747b7` | Hana Mahmutović | docs: audit backend requirements and propose shared contracts |
| `2bb9b9aca6f6ab9ba1add044d53bf458b0b4d779` | `75c9937c5e885b66602ff95993d818eea9bf0b5f` | Hana Mahmutović | fix(backend): redact operational diagnostics and bound correlation IDs |
| `292acba0e06846a44fdb183eef4b7e64936bbd97` | `2dde8f0084bc1b7085a167c5c93981939da1505d` | Hana Mahmutović | fix(auth): require configured API audience outside Development |
| `42a13d546fb42a64288445f8715dd32a2b4df007` | `21ff378efa6fcd7b83d2a3f8d486bb1e6fd0c5c6` | Hana Mahmutović | fix(notifications): encode business messages and correct application links |
| `e491a6e312c78013da4ee0da2cf7c666858d4158` | `ba757589bbecfc53de09a89d922c827195256ed2` | Hana Mahmutović | fix(hosting): respect migration opt-in and align backend entrypoint |
| `927532680e28b684d5905e58cad6f4627b0f0eb9` | `44885b25db6f4d6eda1bf3949e6d680aae71a2a5` | Hana Mahmutović | fix(import): enforce field bounds and reject malformed registry workbooks |
| `99ccca18441c6e98e489326701a80122c044baec` | `f45155f31db2a881c4cf2d09e9591d33cde2b06e` | Hana Mahmutović | fix(codebooks): preserve stable keys and guard referenced deactivation |
| `872ddaa5aae2736b84f2eb7763ea8dcb2bb61d54` | `51922dc376101e4cedd8c4998572ea1d247e59a2` | Hana Mahmutović | fix(logging): keep business and mail data out of operational events |
| `05b63b4a3d0e44a65eb1b2b72727d03b331f6b83` | `307ff1914b354ef1cdcec2d2be3637244c861062` | Hana Mahmutović | docs: record backend evidence and approval-gated security proposals |
| `7f2585d4f1aa7f1920017717545a5eccccced913` | `9f69a21c16aa4d82fc9c7d711b7671a0a6905629` | Hana Mahmutović | fix(verification): enforce subject-bound four-eyes and atomic audit |
| `5735ba0d54aa999d438d183f425956a188992263` | `4e449f1603e9a1395962fcd6d5a6755b87fae741` | Hana Mahmutović | fix(period): scope requests by subject and serialize responses |
| `73e4d7fc3422e017b888664a85e8f5392a97b20f` | `47f57be4f127483d9d6d5c720957bfe57f657d43` | Hana Mahmutović | docs: publish additive frontend contracts and S2 S3 evidence |
| `6ababe7bfeafc189b53cf3fe9354c00f6d67e211` | `81de77449f5d5dd1f9ea2c79f84cd0ec12a1d21c` | Hana Mahmutović | feat(authorization): add atomic role requests and recipient inbox |
| `2f6da19a04f68a76f8205245028a5075ae2942ac` | `cdd630d25591023f45972603379b038fafd5b3ff` | Hana Mahmutović | test(authorization): verify role changes and verified-email boundary |
| `b27a6d4737d1e3b506e8d1e2855389a7d1218fe3` | `4d993d1a79f83179730bcf9610cd674131333356` | Hana Mahmutović | fix(authentication): cap sessions and add CSRF-protected logout |
| `96cca27712bdabf9ed3aa25eff6973dcfeaa82ee` | `dd61be52b39097602aa958d3c5890e9f9042d34b` | Hana Mahmutović | feat(users): complete profile management and normalized identity safeguards |
| `2d29ebcde09c627d794b0b26712c44b741167372` | `c84ddbc7c8986a0c75b7cfd18dba8a39ac8e2c83` | Hana Mahmutović | feat(localization): add runtime codebooks and transactional resource updates |
| `05d173b876313004fb16ab765c7700d82be47a27` | `b42138b71d7c1b9556092e8f5c3a6fc624e15e95` | Hana Mahmutović | feat(registry): add source-backed GCC and paged bank lookups |
| `69fabd59ba671212fa910690bfacac9da8669a97` | `9e3202672b9e97d3692b5ca6af1c8980641f7a9f` | Hana Mahmutović | fix(registry): roundtrip workbook data and persist audit atomically |
| `a61ebc4086cb08001ce5286f64ee2127ed45e140` | `a831a54a997c12292624898c7971a2e49ce0f9f3` | Hana Mahmutović | fix(registry): validate reference choices and bound paged reads |
| `11d852202ab34935bff062493d3a2e8cefa50dbc` | `b2774c35a3d8f900c3f03f30d235354f060bb8e7` | Hana Mahmutović | feat(capital): preserve immutable effective-date revisions and as-of limits |
| `61acce321bdab2910bf4d9df11b28284b2589700` | `f82c3129e0177412b52a59ff160b83d6600892c0` | Hana Mahmutović | feat(reporting): validate snapshot provenance and add safe monthly finance source |
| `d51246827360aa85063e853a732462cce263c083` | `0df282ccb045cc11c85e9d06ae13a6fcef256f8e` | Hana Mahmutović | feat(limits): add transactional workbook import and generated-ID audit |
| `4e1748a2b5dd6203beb1851185ce8a51879710ca` | `d39d6a955975159b7774e83d7928962d7059a6d7` | Hana Mahmutović | fix(period): commit workflow audit and recipient notifications atomically |
| `24b9351ef17a19f10e2afb21a1463ad2a4ce7595` | `aa0739dfe77407bbbac3329c273e25c1293e225e` | Hana Mahmutović | fix(auth): preserve opaque subject identity and owner isolation |
| `5b03a83bfe198677463e49d0cbd012de2a5de8c0` | `22c00909a28e1b317c27c4ab025294ade6fd0197` | Hana Mahmutović | fix(operations): verify schema readiness and expose safe worker diagnostics |
| `4083da135c99cee20f4efa3a0cf90b90655901be` | `de59313128d6cf0acafbfe39df61c96f3b914126` | Hana Mahmutović | fix(audit): preserve stable actors in user administration |
| `28356cfd0698362a90280c17355700b9d5b68941` | `47cd628679b68234e6dd639366ff7df928cc67b7` | Hana Mahmutović | feat(roles): maintain the database permission catalog safely |
| `fc32b69df495103d0e7013e6785bb87f2533e4c8` | `d70785efff058d555bce5830e7bfaf2228519707` | Hana Mahmutović | test(performance): prove bounded paged user queries on SQL |
| `5123e2c77f7c449ff82fba88e505560dafcd9d54` | `9b98423237b7b8737915c62504d4f8eabf3bcef4` | Hana Mahmutović | feat(localization): resolve business errors through runtime language packs |
| `23a5a787465479b8896f5b5a72da0577787b7275` | `5a0c23c426662931086f4a3214e55946f926f753` | Hana Mahmutović | feat(localization): scope runtime controller and field messages per request |
| `0236d57fb6352c54cddefd3aeb01e78d86ffa8de` | `3266b3cc37ed8695356d5f4c2624c41493950575` | Hana Mahmutović | feat(localization): cover service validation and workbook feedback |
| `4fb998a319caf881d27cbbb4b9983d97049d2095` | `8923e44ad73f85febc7a7e4b31f5ff040a4612e6` | Hana Mahmutović | fix(registry): require explicit legal residency in JSON requests |
| `a524fd135251f9d6159097cd8b52042ac82eec3b` | `f0953abb5f95f74f5b93e87bc8b52fd9e93d3506` | Hana Mahmutović | feat(reporting): capture registry classification in new snapshots |
| `6de7cc7445637e2db6d06c6dc00c0ac1f4e07ccc` | `be832cf6cecfd549c743c198332130f57a0a11b0` | Hana Mahmutović | fix(logging): redact period lock request identifiers |
| `d2689de9bddb0acc7229df06cb3d603ca21da457` | `49cc29abfc402ecbbb81eabc7a93cfe965b637e3` | Hana Mahmutović | fix(import): reject codebook formulas before reading cell values |
| `82760bb513f6fa2efe2f9b970bb612b79d509e77` | `206cb99ab7127778fe67670372d28ff705d053d8` | Hana Mahmutović | docs: close backend verification and record deployment dependencies |
| `22f0376ccb6cc79d4d79ada1c967dc7d776d6465` | `d386612a52bffaae5ab92161fef990e6d343bded` | Hana Mahmutović | docs: finalize backend contracts and integration handoff |
| `bddc9136f948c7d8a3c1c3f141d1e461fcc09f5d` | `855aeac16319a5ec153976c1b198d322da9bee87` | Hana Mahmutović | fix(audit): handle extreme pagination and calendar bounds |
| `0cf596d5d02739479e70f5ac7e700c417ca2f0dc` | `cdea8575758af27627fe4c611325efccd438727e` | Hana Mahmutović | docs: record independent backend audit and production blockers |


## Commands and final publication gate

```powershell
python -m unittest discover -s scripts/security -p 'test_*.py'
python scripts/security/scan_git_secrets.py --ref HEAD
dotnet restore RelatedPartiesRegister.slnx --configfile NuGet.config
dotnet test tests/Backend.UnitTests -c Release --no-restore -m:1 -nr:false
$env:RBBH_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true'
dotnet test tests/Backend.IntegrationTests -c Release --no-restore -m:1 -nr:false
dotnet build Backend/RelatedPartiesRegister.csproj -c Release --no-restore -m:1 -nr:false -p:RunAnalyzers=true
dotnet publish Backend/RelatedPartiesRegister.csproj -c Release --no-restore --no-build -o artifacts/backend-release/publish
git diff --check
```

Only disposable GUID LocalDB databases are used. Development smoke explicitly overrides SQL settings to blank/InMemory, migrationsfalse and existing Development auth, binding loopback only. A final complete reachable-history scan is required after the publication commits and before an explicit single-ref non-force push. Original feat/codex-backend-finalization must never be included in that push. The final delivery message records remote/PR results; preparation is not itself a claim that publication succeeded.

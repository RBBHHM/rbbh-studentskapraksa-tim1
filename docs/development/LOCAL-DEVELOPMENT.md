# Local development

Use the two explicit modes and commands in the [root README](../../README.md):

1. Test-only BrowserHost with `BROWSER_FIXTURE_ROLE=admin` for seeded isolated data.
2. Real Backend and Frontend processes with approved SQL/identity/mail configuration.

All RBI runtime dependencies are tracked source projects under `Frontend/Platform`.
Restore the canonical solution using the root `NuGet.config`. Startup does not
apply SQL migrations or seed real databases. Use the approved database workflow
before starting against SQL. Do not use test identities for production.


Backend-only validation while frontend integration is pending:

```powershell
dotnet test tests/Backend.UnitTests -c Release --no-restore
$env:RBBH_TEST_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true"
dotnet test tests/Backend.IntegrationTests -c Release --no-restore
```

SQL tests use disposable GUID-named databases; never point that variable at a
shared UAT/PROD server or catalog. The [backend handoff](../BACKEND-FRONTEND-CONTRACT-V1.md)
and [configuration guide](../configuration/ENVIRONMENT-VARIABLES.md) cover the new
role-request, localization, bank-source and capital-history contracts. Real SSO,
source reconciliation and deployment validation remain separate gates.

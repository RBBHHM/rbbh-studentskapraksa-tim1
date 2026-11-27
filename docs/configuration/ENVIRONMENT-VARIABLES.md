# Environment configuration

Use `__` for nested environment keys. Keep secrets outside Git. Backend dotenv is
loaded from its working directory: the optional local example is `.env.example`;
copy it to `Backend/.env` for `dotnet run --project Backend/RelatedPartiesRegister.csproj`.
The Blazor frontend does not load that file. Prefer its existing user-secrets ID
for local identity credentials. See root README for seeded demo versus real app modes.

## Backend

| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` allows explicit local fallbacks; Production fails closed. |
| `Database__ServerName`, `Database__Name` | SQL target. Leave both blank only for Development InMemory. |
| `Database__IntegratedSecurity`, `Database__User`, `Database__Password` | Integrated identity or SQL login. Use least privilege and trusted TLS. |
| `Database__ApplyMigrations` | Defaults false; startup does not migrate or seed production data. |
| `KeycloakSettings__Enabled` | Development-only local identity when false; no seeded Admin or real SSO. Outside Development authentication must be enabled and fully configured. |
| `KeycloakSettings__Issuer` | Approved HTTPS issuer. |
| `KeycloakSettings__ClientId`, `KeycloakSettings__ClientSecret` | Backend confidential client. Required with enabled production authentication. |
| `KeycloakSettings__PublicIssuer` | Optional approved public issuer when metadata addressing differs. |
| `KeycloakSettings__Audience` | Required API access-token audience in UAT/PROD and every environment except Development. Configured values are validated in Development too. Only unconfigured Development permits the documented local exception. |
| `KeycloakSettings__AdminClientId`, `KeycloakSettings__AdminClientSecret` | Read-only exact identity lookup service account; AdminClientId is selected independently from the OIDC client (ClientId is the compatibility fallback). Never grant realm-role writes. |
| `Email__From`, `Email__SendEmailWithHtmlBodyUrl` | Approved sender and full HTML mail endpoint. Empty values select demo delivery in Development/Testing. |
| `Application__PublicUrl` | Frontend URL for email buttons and the Raiffeisen logo. Use the user-accessible HTTPS address in UAT/Production. Development defaults to https://localhost:5002. |
| `Application__MailLogoUrl` | Optional HTTPS URL for the approved email logo, accessible to recipients’ mail clients. Defaults to the logo hosted by the frontend. |
| `ReverseProxy__KnownProxies__0`, `ReverseProxy__KnownNetworks__0`, `ReverseProxy__ForwardLimit` | Actual trusted peers/CIDRs and positive hop limit. Production rejects missing trust or unrestricted networks. |
| `AllowedHosts` | Explicit application hosts; Production rejects wildcard trust. Probe Host must match. |
| `Cors__AllowedOrigins__0` | Approved browser origins only; no automatic broadening. |
| `IdentityBinding__AllowVerifiedEmailFirstLink` | Approval-gated initial identity linkage; keep false pending production reconciliation. |

### Backend module configuration

| Setting | Purpose |
| --- | --- |
| `Localization__LanguagesCodebook`, `Localization__LanguagePacksCodebook`, `Localization__DefaultLanguage` | Application-specific codebook names (defaults Languages/LanguagePacks) and default BS. No DP_ naming requirement. |
| `BankReference__Enabled`, `BankReference__ServerName`, `BankReference__Database` | DataHub settings. Defaults in backend appsettings are enabled, server `datahub` and database `kod`. The connection uses the normal database authentication settings. |
| `BankReference__Customers__Table`, `BankReference__Customers__Columns__{TaxNumber,RegistrationNumber,Name}` | Overrides for `dbo.komitent` and its `porez_broj`, `matbroj` and `naziv` mappings. |
| `BankReference__Gcc__Table`, `BankReference__Gcc__Columns__{Number,Name}` | Overrides for `glava.dbo.grupekom`, `brojg` and `naziv_grupe`. |
| `FinanceMonthly__Enabled`, `FinanceMonthly__ConnectionString`, `FinanceMonthly__CustomerKeyColumn`, `FinanceMonthly__Currency` | Approved source connection/key and confirmed BAM currency. Disabled local snapshots remain explicitly non-authoritative. |
| `FinanceMonthly:MonthlyTables` | Exact approved YYYY-MM to fully qualified monthly-table dictionary through approved configuration. Never guess suffixes. |
| `FinanceMonthly__BalanceColumn`, `FinanceMonthly__OffBalanceColumn` | Documented components Bilans_exponiranost_bez_Ulaganja_sredstava_poreza and VB_Ukupno. Real samples/spreadsheet totals must reconcile before production use. |

DataHub reuses `Database__IntegratedSecurity` and, for SQL authentication, `Database__User` and `Database__Password` from the environment. The account needs SELECT access to the configured source tables. The legal entity form searches by name, registration number or tax number inside its Name field. The source column `matbroj` supplies the registration number shown in the form. When the source is enabled, saving or importing checks that value using an exact lookup. For residents without a registration number it checks an exact tax-number match and fills the registration number when found. The legacy Matbroj column remains available to existing exports and reports.

Runtime localization needs **two codebooks alongside the application's other codebooks**: supported languages (BS/EN/DE) and language packs with active, language code, resource key, label, helper and placeholder. Default migrations seed missing reference resources without overwriting existing translations. For custom names, provision the same logical contract through normal application codebook administration. New keys use lowercase semantic dot notation; existing legacy key casing remains readable. Import/export schema identifiers remain canonical compatibility values.

The backend cookie has an absolute eight-hour ceiling with no sliding extension. Realm/token expiration still applies; real eight-hour browser/refresh behavior requires frontend/Keycloak acceptance. POST logout requires the antiforgery token from the existing auth endpoint; legacy GET logout is deprecated pending coordinated consumer migration. Do not enable verified-email first-link or run a subject backfill to bypass production reconciliation.

SQL readiness requires connectivity and no pending migrations; the probe never applies DDL. Apply reviewed migrations through the approved database workflow before traffic. `/metrics` requires existing administrative read permission; live/ready status probes remain anonymous. Real alert thresholds and authenticated scrape integration belong to deployment owners.

See the [backend contract](../BACKEND-FRONTEND-CONTRACT-V1.md) for DTOs/statuses and the [current backend matrix](../BACKEND-FINALIZATION.md) for tested scope and external dependencies.

## Frontend Blazor

| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Local Development permits signed-out startup; Production requires authentication. |
| `Apis__BaseUrl` | Real backend origin. A blank value gives unavailable clients, not a working register. |
| `Authentication__Authority` | Approved HTTPS Keycloak realm. |
| `Authentication__ClientId`, `Authentication__ClientSecret` | Confidential frontend client, configured together with Authority. |
| `AllowedHosts` | Approved frontend hosts, consistent with ingress/probe requests. |

Production authentication requires an approved registered shared `IDistributedCache`
and persisted/protected Data Protection keys. Environment variables alone do not
register that infrastructure. Local Development cache is not the production solution.
Frontend proxy trust must also be approved and integrated for the real ingress.

## Production boundary

For UAT/PROD, obtain the API audience from the realm owner and configure
`KeycloakSettings__Audience` through deployment configuration. The access token
sent to this API must contain that value in its `aud` claim (a single value or
one member of an audience array). Configure the realm/client audience mapper
accordingly. A missing/wrong audience is rejected with401; missing API audience
configuration prevents startup outside Development. Do not disable validation to
work around a mapper mismatch.

This API audience is distinct from the OIDC confidential client ID. The existing
authorization-code/PKCE login, issuer/signature/lifetime validation and OIDC ID-token
audience validation remain in place. Local signed-token HTTP tests exercise the
API bearer handler; actual realm login, mapper output and token forwarding must
still be verified against the approved UAT/PROD Keycloak configuration.

Production requires real SQL, Keycloak audience/claims/redirects, `email_verified`,
subject-link backfill approval, trusted CA chains and mail delivery verification.
Do not enable first-link fallback or relax validation to bypass those checks.
Deployment prerequisites remain in [production readiness](../PRODUCTION-READINESS.md).

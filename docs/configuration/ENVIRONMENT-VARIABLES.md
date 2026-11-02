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
| `KeycloakSettings__Enabled` | May be false only in Development; not an authenticated demo mode. |
| `KeycloakSettings__Issuer` | Approved HTTPS issuer. |
| `KeycloakSettings__ClientId`, `KeycloakSettings__ClientSecret` | Backend confidential client. Required with enabled production authentication. |
| `KeycloakSettings__PublicIssuer` | Optional approved public issuer when metadata addressing differs. |
| `KeycloakSettings__Audience` | Required API access-token audience in UAT/PROD and every environment except Development. Configured values are validated in Development too. Only unconfigured Development permits the documented local exception. |
| `KeycloakSettings__AdminClientId`, `KeycloakSettings__AdminClientSecret` | Configure only where the existing Keycloak administration integration requires them. - same client id | 
| `Email__Provider` | `demo` for isolated local work; configure approved HTTP API or SMTP for real delivery. |
| `Email__AdminEmail`, `Email__HrEmail`, `Email__From`, `Email__AppBaseUrl` | Approved recipients, sender and links; do not test with real mail inadvertently. |
| `Email__SendEmailWithHtmlBodyUrl` | Existing HTTP mail endpoint when that provider is selected. |
| `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__EnableSsl`, `Email__Smtp__Username`, `Email__Smtp__Password`, `Email__Smtp__FromAddress`, `Email__Smtp__FromName` | SMTP provider configuration. |
| `ReverseProxy__KnownProxies__0`, `ReverseProxy__KnownNetworks__0`, `ReverseProxy__ForwardLimit` | Actual trusted peers/CIDRs and positive hop limit. Production rejects missing trust or unrestricted networks. |
| `AllowedHosts` | Explicit application hosts; Production rejects wildcard trust. Probe Host must match. |
| `Cors__AllowedOrigins__0` | Approved browser origins only; no automatic broadening. |
| `IdentityBinding__AllowVerifiedEmailFirstLink` | Approval-gated initial identity linkage; keep false pending production reconciliation. |

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

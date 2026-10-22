# RBBH.RelatedPartiesRegister.Web

## Your application in one minute

- **DesignSystem** is the shared UI package. **RBI components are compiled in the
  Razor Class Library package; they are not copied into your project.** Styles,
  fonts and icons resolve through `_content/RBI.DesignSystem`.
- **Components** contains your pages and layouts: MainLayout, NavMenu, HeroBar,
  Home (`/`), Audit Log (`/audit-log`), My Profile (`/profile`), Error and Not Found.
- **Api** calls your separate backend. The frontend has no database, EF entities,
  backend business services or audit persistence.
- **Localization**, when selected, contains application text. **Program.cs**
  wires selected features; **appsettings** is the environment configuration seam.

Set **`Apis:BaseUrl`** to your local backend or approved UAT/PROD gateway. It must
end in `/` (including any gateway path prefix); HTTPS is required outside
Development. Do not hardcode URLs in clients or duplicate named-client BaseUrls.
Audit uses the separate backend's `api/audit?page=1&pageSize=10` contract with
`{ items: [...], page: 1, pageSize: 10, totalCount: ... }`. Generate the separate
`rbbh-api` starter for the matching development contract; production persistence
and application authorization remain backend-owned integration work.
Protected clients forward the signed-in user's token only when auth is selected.

With no backend address, Development returns a safe empty audit result and other
environments show an explicitly unavailable state. The application still starts;
no fake audit records are generated and configured failures never silently become
local success. The profile uses real Blazor authentication state when selected,
or clearly says authentication is unconfigured. Test identities belong only in
external test fixtures, never this starter.

Run the Interactive Server application with (auth-enabled variants first require the configuration below):

```powershell
dotnet run
```

The launch profile does not fix a local port. Use the address printed by the host, or supply `--urls` explicitly when coordinating several applications.

RBI libraries come from the approved company NuGet feed and are consumed only through `PackageReference`. The app uses package-owned DesignSystem CSS and JavaScript through `_content/RBI.DesignSystem`; keep only app-owned assets in `wwwroot`.

## Build and deployment

The generated repository owns its `Dockerfile` and two GitHub workflows:

- `build-and-push-image.yml` builds with the approved internal .NET 10 images and pushes only an immutable version/SHA tag;
- `deploy-to-ocp.yml` validates that immutable image, imports it into an OpenShift `ImageStream`, and idempotently applies the Deployment and Service. It never creates or overwrites a platform-owned Route/TLS policy.

Configure these values in each GitHub Environment (`Development`, `UAT`, `PROD`):

| Kind | Name | Purpose |
|---|---|---|
| Variable | `ARTIFACTORY_REGISTRY` | Registry host, without scheme |
| Variable | `ARTIFACTORY_REPOSITORY` | Docker repository path |
| Variable | `IMAGE_NAME` | Lowercase image and OpenShift resource name |
| Variable | `OCP_SERVER` | OpenShift API URL |
| Variable | `OCP_PROJECT_NAME` | Target OpenShift project/namespace |
| Secret | `ARTIFACTORY_USERNAME` | Registry service-account username |
| Secret | `ARTIFACTORY_TOKEN` | Registry service-account token |
| Secret | `OCP_TOKEN` | OpenShift deployment token |

Never commit these values or copy them into `appsettings*.json`. The target namespace must provide the platform-managed `ca-certificates` ConfigMap. The workflow mounts it at `/etc/ssl/certs/additional-certs`, runs the container on port `8080`, and probes `/health/live` and `/health/ready`.

Application settings belong to the generated application, not RBI packages. If needed, create `${IMAGE_NAME}-config` and `${IMAGE_NAME}-secrets` in the target namespace; the Deployment imports both when present. Put non-sensitive ASP.NET Core keys in the ConfigMap and credentials in the Secret, using double underscores for nested keys. The workflow deliberately does not invent Keycloak, 3scale, cache, database, route TLS or certificate values. Platform owners remain responsible for namespace access, route/DNS/TLS policy, shared cache, persistent Data Protection keys and WebSocket-capable ingress.

Use this ownership split:

- **Source-controlled non-secret configuration:** only genuinely environment-neutral `appsettings.json` defaults.
- **GitHub/OpenShift environment variables:** API base URLs, identity authority/client ID and other environment-specific non-secrets.
- **Secrets:** client secrets, tokens and credentials.
- **Platform resources:** shared Data Protection storage, approved distributed cache, CA ConfigMap, route, DNS and TLS.

## Hosting

`RBI.Hosting` provides anonymous `/health/live` and `/health/ready` probes plus safe `X-Forwarded-For` and `X-Forwarded-Proto` processing. Call `UseRbiHosting()` before HTTPS redirection. The default trusts only framework-known loopback proxies; production must add its actual trusted proxy addresses or networks through `AddRbiHosting(...)`.

Add application readiness checks with the `ready` tag (`RbiHealthCheckTags.Ready`). Liveness is process-only and never runs dependency checks. The package does not configure HSTS, exception handling, Data Protection, cache, WebSockets or platform topology.

Forms use standard Blazor `EditForm`, `EditContext` and DataAnnotations. `RBI.Forms` is intentionally not included by default; add it only when the application needs generic server-error mapping or guarded asynchronous submission.

## Authentication

### PRODUCTION BLOCKER

Production authentication **cannot start** until the application registers a platform-approved shared `IDistributedCache`. The generated `AddDistributedMemoryCache()` registration is Development-only; there is no silent production fallback. Do not remove this startup guard to deploy.

Multiple replicas also require persistent/shared Data Protection keys, WebSockets/SignalR-capable ingress and an approved affinity/reconnect plan. Shared auth storage is not distributed Blazor circuit storage. Confirm these prerequisites with platform owners before scaling beyond one replica.

Set `Authentication:Authority` and `Authentication:ClientId`, then provide `Authentication__ClientSecret` through user-secrets or the deployment environment. Never commit the client secret.

For OpenShift, place `Authentication__Authority` and `Authentication__ClientId` in `${IMAGE_NAME}-config`, and `Authentication__ClientSecret` in `${IMAGE_NAME}-secrets`. Add environment-specific callback and 3scale/API values through the same split when the application uses them.

`AddDistributedMemoryCache()` is local-development scaffolding only. Before production or multi-instance deployment, replace it with the platform-approved shared `IDistributedCache` provider. Interactive Server requires normal ASP.NET Core hosting with WebSockets/SignalR support; it is not a static-site ZIP.

## Authorization

`RBI.Authorization` is registered with a deny-by-default empty mapping. Define application-owned role-to-permission mappings in `AddRbiAuthorization`; use lowercase semantic names such as `customers.view`. Enforce the same permission with `RbiAuthorize`, `IRbiPermissionService` or `RequireRbiPermission`.

## Localization

Application-owned translations live in `Localization/bs.json`, `en.json` and `de.json`. BS is the default. Use lowercase semantic keys and post culture changes to `/rbi-localization/culture/{culture}` with antiforgery protection.


# RBI.Authentication

## Purpose

Optional confidential OIDC/Keycloak session management and scoped user-token propagation on ApiCore.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

```csharp
// Register an approved shared IDistributedCache first.
builder.Services.AddRbiAuthentication(builder.Configuration);
// On the returned API-client builder:
apiClientBuilder.AddRbiUserAccessToken();
```
Keep UseAuthentication, UseAuthorization and MapRbiAuthenticationEndpoints visible in the host.

## Key contract

Server-side token sessions, secure cookies, refresh coordination and scoped signed-in user token propagation. Do not couple UI/business permission names to this package.

Refresh responses are streamed with a 64 KiB payload limit before deserialization, under the backchannel timeout through body reads. Caller cancellation propagates; provider timeout returns temporary unavailability without logging token payloads.

## Production caveats

PRODUCTION BLOCKER: a shared IDistributedCache must be explicitly registered; no silent production memory fallback. Supply Authentication credentials externally. Replicas need shared Data Protection, SignalR/ingress and an affinity/reconnect plan.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).

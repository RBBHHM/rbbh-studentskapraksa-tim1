# RBI.Authorization

## Purpose

Optional permission checks from a ClaimsPrincipal; independent of Authentication/Keycloak and network calls.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

```csharp
builder.Services.AddRbiAuthorization(options => options
    .MapRole("CustomerReader", "customers.view"));
app.MapGet("/customers", GetCustomers).RequireRbiPermission("customers.view");
```
GetCustomers and role/permission names are application-owned.

## Key contract

IRbiPermissionService, IRbiPermissionSource, RbiAuthorize and RequireRbiPermission share the same permission contract. Unknown/unmapped permissions deny access.

## Production caveats

UI hiding is not security enforcement: apply endpoint checks too. Keep role mappings application-owned; authenticated fallback is an explicit opt-in. Remote permission adapters are not included.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).

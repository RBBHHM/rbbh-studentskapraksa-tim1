# RBI.Hosting

## Purpose

Minimal anonymous process/readiness probes and safe trusted forwarded-header processing; not a deployment platform.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

```csharp
builder.Services.AddRbiHosting();
var app = builder.Build();
app.UseRbiHosting(); // before HTTPS redirection
```
Register application dependency health checks with tags: [RbiHealthCheckTags.Ready].

## Key contract

/health/live is process-only; /health/ready aggregates readiness-tagged checks. Responses contain minimal aggregate status.

## Production caveats

Application/platform owners choose proxies, HSTS, exception handling, cache, Data Protection and SignalR topology. Never clear known proxy/network collections to trust arbitrary forwarded headers.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).

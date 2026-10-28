# RBI.Localization

Resources are discovered once at startup from `Localization/*.json`. Adding
`fr.json` with `"culture": "fr"` makes French available to request localization,
the injected `RbiCultureCatalog`, and Sifrarnik without a second language list.
Restart after adding resources; this is not a filesystem watcher. The generated
localized starter uses the same catalog for its antiforgery-protected switcher.

BS remains the default. Override explicitly with
`services.AddRbiLocalization(options => options.DefaultCulture = "en")`.
The default resource must cover the union of all application keys. Invalid
cultures, missing/mismatched culture declarations, malformed JSON, duplicate
properties/cultures and incomplete default resources fail startup. Other
language gaps retain the existing default-culture fallback and diagnostics.

## Purpose

Optional HTTP-free text localization from application-owned BS/EN/DE JSON resources.

## Source integration

This repository builds this library through ProjectReference; see ../SOURCE.md.

## Minimal setup

```csharp
builder.Services.AddRbiLocalization();
app.UseRbiLocalization();
app.UseAntiforgery();
app.MapRbiLocalizationEndpoints();
```
Create Localization/bs.json, en.json and de.json; inject IRbiLocalizer and use lowercase semantic keys.

## Key contract

BS default; specific-to-neutral-to-BS-to-key fallback. Text formatting follows the active culture; UI components receive translated strings from the application.

## Production caveats

Keep resource key sets aligned. Culture changes use the antiforgery-protected POST endpoint and a local return URL; application resources are not supplied by the package.

See [repository development guidance](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEVELOPMENT.md), [deployment](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/DEPLOYMENT.md) and [public API review](https://github.com/mahmutovichana/rbi-design-core/blob/main/Docs/PUBLIC-API.md).

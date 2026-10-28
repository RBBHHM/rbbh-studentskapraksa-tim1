# Vendored RBI runtime source

Source: https://github.com/mahmutovichana/rbi-design-core (approved local source checkout).
Only runtime dependency closure is imported; no showcase, templates, platform test
harness, runtime Sifrarnik provider, packaging/release scripts or frontend framework
reference app is included.

| Local project | Upstream directory | Source revision |
| --- | --- | --- |
| RBI.DesignSystem | DesignSystem | 8f0f5fe7622df6472ab636c43ba875fa279b0a8f |
| RBI.Hosting | Hosting | same |
| RBI.Forms | Forms | same |
| RBI.Authorization | Auth/Authorization | same |
| RBI.Localization | Localization/Core | same |
| RBI.Authentication | Auth/Authentication | same (unchanged through fdefb8a) |
| RBI.ApiCore | Api | a38b94c (approved authenticated multipart/bounded binary transport patch) |

The five original runtime library receipts identify the first revision. Transport
receipts carry the pre-patch fdefb8a stamp; their approved file transport extension
is preserved from a38b94c. No newer platform UI implementation is substituted.
A file-level integrity manifest is in `SOURCE-MANIFEST.json`; verification results
are in the structural cleanup report under `docs/`. Manifest hashes describe source
bytes before Git checkout line-ending conversion (LF/CRLF is not a runtime patch).

## Local build adaptation

Project filenames, assemblies, namespaces and static-asset base paths are retained.
`RBI.Authentication` points to its adjacent `RBI.ApiCore` project. Versioned public
third-party references replace inherited central versions without changing them:
OIDC 10.0.12, HTTP resilience 10.10.0, PublicApiAnalyzers 5.6.0.
`Directory.Build.props` retains framework/nullability/implicit usings/API analyzer
rules, disables packaging, and omits upstream release-only metadata.
The app excludes `Platform/**` from its own default items; each library compiles
and publishes its own assets. There are no application-specific runtime patches. Imported EOF-only blank lines
were removed where required by git diff --check; the manifest records each case.
Upstream library READMEs are reference contracts; installation is through the
checked-in projects, as described here.

## Project graph

Frontend -> RBI.DesignSystem, RBI.Hosting, RBI.ApiCore, RBI.Authentication,
RBI.Forms, RBI.Authorization, RBI.Localization.
RBI.Authentication -> RBI.ApiCore. The other six runtime projects have no further
RBI project dependencies. Backend has no RBI dependency.

All third-party packages use the root NuGet configuration. No sibling checkout,
locally built package, private RBI feed or cached RBI package is required.
Preserve `RBI.DesignSystem/THIRD-PARTY-NOTICES.txt` with the source/assets.

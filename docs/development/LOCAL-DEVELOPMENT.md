# Local development

Use the two explicit modes and commands in the [root README](../../README.md):

1. Test-only BrowserHost with `BROWSER_FIXTURE_ROLE=admin` for seeded isolated data.
2. Real Backend and Frontend processes with approved SQL/identity/mail configuration.

All RBI runtime dependencies are tracked source projects under `Frontend/Platform`.
Restore the canonical solution using the root `NuGet.config`. Startup does not
apply SQL migrations or seed real databases. Use the approved database workflow
before starting against SQL. Do not use test identities for production.

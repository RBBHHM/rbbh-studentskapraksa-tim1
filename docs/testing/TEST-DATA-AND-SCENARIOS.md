# Test data and scenarios

Use the root README's test-only BrowserHost for a realistic, isolated local demo.
Set BROWSER_FIXTURE_ROLE to admin, gost, kapital, PL, limiti or FL. The host uses
an isolated InMemory database, real application clients/controllers and synthetic
identities; it never executes production startup, real mail, SQL or Keycloak.

Native tests under tests/Web.Blazor.Browser reset and seed scenario-specific
physical/legal persons, limits, capital, reports, users, period, code lists,
notifications and audit data. They check role gates, mutations, validation,
BS/EN, light/dark, keyboard behavior, Axe and 375px responsiveness. Run all six
roles with workers=1 and retries=0. Browser identities are test-only.

Backend unit/integration projects live under tests/Backend.UnitTests and
tests/Backend.IntegrationTests. SQL tests require RBBH_TEST_SQLSERVER pointing to
a disposable server with no Initial Catalog. They create/drop uniquely named
databases, including last-Admin concurrency and readiness checks. Never point the
test suite at production. Skipped tests are not passes.

The backend does not apply migrations or seed a real database at startup. See
../database/DB-CHANGE-WORKFLOW.md before using real SQL data.

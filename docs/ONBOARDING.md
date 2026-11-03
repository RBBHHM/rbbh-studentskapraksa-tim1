# Onboarding

Everything a new developer needs, in order. The README holds the reference detail (configuration, database workflow, operations).

## 1. Set up (about 15 minutes)
1. Install the .NET SDK 10 and Git. Node 22 is needed only for browser tests.
2. Clone the repository and run `dotnet restore RelatedPartiesRegister.slnx`, then `dotnet build` and `dotnet test`. All tests must pass before you change anything.
3. Run the seeded demo (README, "Running locally", mode A) with `BROWSER_FIXTURE_ROLE=admin` and click through every module. Repeat with `gost` to see the read-only Guest view.

## 2. Where things are
| You want to change | Look in |
| --- | --- |
| A screen | `Frontend/Components/Pages` |
| A form or shared control | `Frontend/Components/Registry` (`LiveValidation`, `FormSummary`, `LimitEntityPicker` ...) |
| Calls to the API | `Frontend/Api` (clients) and `Frontend/Security` (permissions) |
| Visible text | `Frontend/Localization/bs.json` and `en.json` |
| Styles | `Frontend/wwwroot/app.css` (RBI design tokens) |
| Business rules, endpoints | `Backend` |
| Database scripts | `docs/database/changes` |

## 3. Rules that the build enforces
- **No hardcoded text.** Every visible string is `T["resource.key"]`. Add the key to both `bs.json` and `en.json`.
- **No hardcoded options.** Dropdowns and multi-selects come from code lists. A literal `<option>` or a new static business list fails `FrontendSourceGuardTests`.
- **Forms validate while typing.** Use `LiveValidation` and `FormSummary`; the primary button stays disabled while the form is invalid, and a field message is never repeated in a summary. Do not use `<ValidationSummary>`.
- **No secrets in Git.** Use user-secrets or environment variables. Never write client secrets, tokens or passwords to logs, tests or docs.
- **Four-eyes.** The creator of a record cannot verify it.
- **Roles are data.** Never compare against role names for access decisions; use permissions from the profile.
- **Keyboard and contrast.** Everything must work with the keyboard, and light and dark themes must pass Axe.

## 4. Making a change
1. Branch from the current integration branch; keep commits small and logical.
2. Implement; add or update tests next to the behaviour (xUnit for models and clients, Playwright for flows).
3. Run `dotnet test`, then the Playwright suite for the roles your change affects (full six-role run before a release).
4. Open a pull request. Do not push to the main branch directly.

For a database change follow the README "Database" steps; the application never migrates on startup.

## 5. Running the browser tests
```powershell
cd tests/Web.Blazor.Browser
npm ci; npx playwright install chromium
$env:BROWSER_FIXTURE_ROLE = "FL"
npx playwright test physical-persons.spec.ts --workers=1 --retries=0
```
The role variable picks the identity the seeded host signs in with; run a spec with the role it was written for.

## 6. Where to get help
Requirements: `docs/POPRAVKE.txt` and `docs/FRONTEND-COVERAGE.md`. Platform (RBI) libraries: the README in each `Frontend/Platform/RBI.*` folder. Database questions: `it-dba@raiffeisengroup.ba`.

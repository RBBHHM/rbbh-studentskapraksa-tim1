# OCP backend i IIS frontend

Backend i frontend su dva nezavisna deployment artefakta. OCP image sadrži samo backend iz `RelatedPartiesRegister/`, dok se statički React paket gradi iz `src/Web` i predaje IIS administratoru kao ZIP.

Tri fajla u `.github/workflows` ostaju byte-po-byte jednaka DataProducts workflowima. U svakom GitHub Environmentu postavite:

| Varijabla | Vrijednost |
|---|---|
| `PROJECT_PATH` | `RelatedPartiesRegister` |
| `SOLUTION_PATH` | `RelatedPartiesRegister.csproj` |
| `NUGET_CONFIG_PATH` | `nuget.config` |

Build workflow koristi `RelatedPartiesRegister/Dockerfile` sa korijenom repozitorija kao Docker contextom. CodeQL izvršava restore i build nad `RelatedPartiesRegister/RelatedPartiesRegister.csproj`. Connection string, Keycloak, certifikati i ostale tajne dolaze iz OCP Secret/ConfigMap resursa i ne upisuju se u image ili Git.

Frontend paket se pravi komandom `pnpm publish:iis` iz `src/Web`. IIS raspakuje
ZIP, nudi HTTPS i SPA fallback te prosljeđuje relativne `/api`, `/health` i
`/openapi` rute prema OCP servisu. Keycloak PKCE redirect obavlja browser.

ZIP nema `.dll` jer React nije serverski proces: IIS vraća već izgrađeni HTML,
CSS, JavaScript, fontove i slike. `app-config.js` sadrži samo javne runtime
vrijednosti i može se promijeniti bez rebuilda. Na IIS-u nisu potrebni Node,
pnpm ni .NET. Backend `.dll` postoji samo unutar OCP imagea.

```text
Browser → IIS statički frontend / SPA fallback
        → /api kroz IIS ARR → OCP Route → RelatedPartiesRegister → SQL Server
        → Keycloak kroz Authorization Code + PKCE
```

OCP database ConfigMap/Secret koristi `Database__ServerName`,
`Database__Name`, `Database__IntegratedSecurity`, `Database__User` i
`Database__Password`; puni connection string se ne postavlja.

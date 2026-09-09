# OCP backend i IIS frontend

Backend i frontend su dva nezavisna deployment artefakta. OCP image sadrži samo backend iz `RelatedPartiesRegister/`, dok se statički React paket gradi iz `src/Web` i predaje IIS administratoru kao ZIP.

Tri fajla u `.github/workflows` ostaju byte-po-byte jednaka DataProducts workflowima. U svakom GitHub Environmentu postavite:

| Varijabla | Vrijednost |
|---|---|
| `PROJECT_PATH` | `RelatedPartiesRegister` |
| `SOLUTION_PATH` | `RelatedPartiesRegister.csproj` |
| `NUGET_CONFIG_PATH` | `nuget.config` |

Build workflow koristi `RelatedPartiesRegister/Dockerfile` sa korijenom repozitorija kao Docker contextom. CodeQL izvršava restore i build nad `RelatedPartiesRegister/RelatedPartiesRegister.csproj`. Connection string, Keycloak, certifikati i ostale tajne dolaze iz OCP Secret/ConfigMap resursa i ne upisuju se u image ili Git.

Frontend paket se pravi iz `src/Web`, uz obavezni direktni OCP backend origin:

```powershell
pnpm publish:iis -- --backend-origin http://OCP-BACKEND-ROUTE
```

Koristite direktnu backend Route adresu bez 3scale path prefiksa. Skripta gradi
production localization manifest, generiše same-origin `app-config.js` i
ugrađuje backend origin u paketovani `web.config`. IIS raspakuje ZIP, nudi HTTPS
i SPA fallback te prosljeđuje relativne `/api`, `/health`, `/openapi`,
`/authentication` i OIDC callback rute prema OCP servisu. Backend obavlja
confidential-client OIDC code exchange i vraća HttpOnly cookie.

Na IIS serveru moraju biti omogućeni ARR Proxy i URL Rewrite server varijable
`HTTP_X_FORWARDED_HOST` i `HTTP_X_FORWARDED_PROTO`.

ZIP nema `.dll` jer React nije serverski proces: IIS vraća već izgrađeni HTML,
CSS, JavaScript, fontove i slike. `app-config.js` sadrži samo javne runtime
vrijednosti i može se promijeniti bez rebuilda. Na IIS-u nisu potrebni Node,
pnpm ni .NET. Backend `.dll` postoji samo unutar OCP imagea.

```text
Browser → IIS statički frontend / SPA fallback
        → /authentication i /api kroz IIS ARR → OCP RelatedPartiesRegister
        → Keycloak kroz serverski Authorization Code tok → SQL Server
```

OCP database ConfigMap/Secret koristi `Database__ServerName`,
`Database__Name`, `Database__IntegratedSecurity`, `Database__User` i
`Database__Password`; puni connection string se ne postavlja.

## OCP, Keycloak i 3scale

Backend OCP Route mora nuditi plain HTTP prema aplikaciji. Aplikacija ne radi
TLS terminaciju i ne preusmjerava Swagger/API zahtjeve na HTTPS; javni HTTPS i
certifikat završavaju na 3scale ruti:

`https://related-parties-register-front-3scale-apicast-production.apps.ocp4test2.mbdom.rbbh/related-parties-register`

U OCP ConfigMap postavite `KeycloakSettings__Enabled=true`,
`KeycloakSettings__Issuer=https://keycloak-test-rhsso.apps.ocp4test2.mbdom.rbbh/auth/realms/rbbh`,
`KeycloakSettings__Audience=c1fc0fe1` i
`KeycloakSettings__AdminClientId=c1fc0fe1`. Vrijednost
`KeycloakSettings__AdminClientSecret` postavlja se isključivo kroz OCP Secret.

OIDC client ID i secret postoje samo na backendu kao
`KeycloakSettings__ClientId` i `KeycloakSettings__ClientSecret`; secret se
postavlja kroz OCP Secret. React ne dobija Keycloak token niti secret. Keycloak
client mora dozvoliti javni IIS URL sa `/signin-oidc` callback putanjom.

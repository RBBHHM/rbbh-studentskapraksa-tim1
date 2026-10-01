# OCP backend i IIS frontend

Backend i frontend su dva nezavisna deployment artefakta. OCP image sadrži samo backend iz `RelatedPartiesRegister/`, dok se statički React paket gradi iz `src/Web` i predaje IIS administratoru kao ZIP.

Tri fajla u `.github/workflows` ostaju byte-po-byte jednaka DataProducts workflowima. U svakom GitHub Environmentu postavite:

| Varijabla | Vrijednost |
|---|---|
| `PROJECT_PATH` | `RelatedPartiesRegister` |
| `SOLUTION_PATH` | `RelatedPartiesRegister.csproj` |
| `NUGET_CONFIG_PATH` | `nuget.config` |

Build workflow koristi `RelatedPartiesRegister/Dockerfile` sa korijenom repozitorija kao Docker contextom. CodeQL izvršava restore i build nad `RelatedPartiesRegister/RelatedPartiesRegister.csproj`. Connection string, Keycloak, certifikati i ostale tajne dolaze iz OCP Secret/ConfigMap resursa i ne upisuju se u image ili Git.

Frontend paket se pravi iz `src/Web`, uz obavezni backend i Keycloak origin:

```powershell
pnpm publish:iis -- --backend-origin <BACKEND-ORIGIN> --keycloak-origin <KEYCLOAK-PUBLIC-ORIGIN>
```

`--backend-origin` je UVIJEK direktna backend OCP Route adresa (bez 3scale
path prefiksa), u oba okruženja — IIS-ov interni reverse proxy za login/API
saobraćaj ove aplikacije nikad ne ide kroz 3scale (vidi razlog i primjere
ispod). `--keycloak-origin` je javni Keycloak origin (samo scheme+host, bez
`/auth/realms/...` putanje) koji IIS koristi da vrati tačan javni host u
`Location` header pri Keycloak redirect-u.

UAT (direktna Route, bez 3scale, 2 proxy hop-a: IIS → OCP Route → app):

```powershell
pnpm publish:iis -- --backend-origin http://related-parties-register-rbbh-uat-t24.apps.ocp4test2.mbdom.rbbh --keycloak-origin https://keycloak-test-rhsso.apps.ocp4test2.mbdom.rbbh
```

PROD (direktna Route, bez 3scale, 2 proxy hop-a: IIS → OCP Route → app):

```powershell
pnpm publish:iis -- --backend-origin <PROD-OCP-ROUTE-ORIGIN> --keycloak-origin https://keycloak-prod-rhsso.apps.ocp4prod.mbdom.rbbh
```

`<PROD-OCP-ROUTE-ORIGIN>` je PROD analog UAT-ove Route adrese iznad (npr.
`http://related-parties-register-rbbh-prod-XXXX.apps.ocp4prod.mbdom.rbbh`) —
zatražiti tačnu vrijednost od OCP administratora prije PROD publish-a.

**Zašto IIS ne ide kroz 3scale, ni u PROD-u:** 3scale APIcast Product za ovu
aplikaciju je podešen na OIDC bearer-token validaciju (potvrđeno testom:
slanje `Authorization: Bearer <token>` daje `403 Authentication failed`, a ne
`Authentication parameters missing` kao kod nepoznatih parametara — 3scale
dakle validira postojeći Keycloak access token, ne pokreće login). IIS-ov
`/authentication/login` i `/signin-oidc` callback rade upravo suprotno: PRVI
poziv pokreće Keycloak login PRIJE nego što ijedan token postoji, pa bi 3scale
takav poziv odbio sa `403` prije nego što bi stigao do backend-a. Zato IIS ide
direktno na OCP Route za sav interni saobraćaj ove aplikacije; 3scale ostaje
rezervisan isključivo za eksterne API konzumente koji već posjeduju token (vidi
sekciju "OCP, Keycloak i 3scale" ispod).

Skripta gradi production localization manifest, generiše same-origin
`app-config.js` i ugrađuje oba origina u paketovani `web.config`. IIS
raspakuje ZIP, nudi HTTPS i SPA fallback te prosljeđuje relativne `/api`,
`/health`, `/openapi`, `/authentication` i OIDC callback rute direktno prema
`--backend-origin` (OCP Route, bez 3scale). Backend obavlja confidential-client
OIDC code exchange i vraća HttpOnly cookie.

Na IIS serveru moraju biti omogućeni ARR Proxy i URL Rewrite server varijable
`HTTP_X_FORWARDED_HOST` i `HTTP_X_FORWARDED_PROTO`.

**Broj proxy hop-ova i `ForwardLimit`:** `Program.cs` postavlja
`ForwardedHeadersOptions.ForwardLimit` na broj reverse-proxy skokova između
klijenta i aplikacije, jer su `KnownProxies`/`KnownIPNetworks` namjerno
isključeni (OCP/IIS adrese su dinamične). Trenutno: `ForwardLimit = 2` (IIS →
OCP Route → app) — isto u UAT-u i PROD-u, jer IIS u oba okruženja ide direktno
na OCP Route. Ako se topologija ponovo promijeni (npr. neko okruženje ubuduće
ipak mora ići kroz dodatni proxy hop), `ForwardLimit` mora se ažurirati da
odgovara stvarnom broju skokova, inače se javni host/protokol netačno
"odmotava" i vraća se bug sa pogrešnim javnim URL-om kroz proxy: middleware sa
`ForwardLimit` većim od stvarnog broja hop-ova je siguran (samo neće imati
dovoljno unosa da ih sve potroši), dok manji `ForwardLimit` od stvarnog broja
hop-ova GUBI originalni javni host.

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
TLS terminaciju; javni HTTPS i certifikat za eksterne konzumente završavaju na
3scale ruti. Ovu 3scale rutu koriste ISKLJUČIVO eksterni API konzumenti koji
već posjeduju Keycloak access token — IIS-ov interni reverse proxy (i u UAT-u
i u PROD-u, `--backend-origin` u `pnpm publish:iis` komandi iznad) ide
direktno na backend Route, van 3scale, jer 3scale Product je podešen na OIDC
bearer-token validaciju i odbija `/authentication/login`/`/signin-oidc` pozive
koji (namjerno) još nemaju token — vidi objašnjenje iznad.

- UAT (samo eksterni konzumenti): `https://related-parties-register-product-3scale-apicast-production.apps.ocp4test2.mbdom.rbbh/related-parties-register`
- PROD (samo eksterni konzumenti): `https://related-parties-register-product-3scale-apicast-production.apps.ocp4prod.mbdom.rbbh/related-parties-register`

U OCP ConfigMap postavite `KeycloakSettings__Enabled=true`,
`KeycloakSettings__Audience` i `KeycloakSettings__AdminClientId` na stvarni
OIDC client ID, i `KeycloakSettings__Issuer` na `<KEYCLOAK-ORIGIN>/auth/realms/rbbh`
gdje je `<KEYCLOAK-ORIGIN>` isti origin koji se koristi kao `--keycloak-origin`
iznad:

- UAT: `KeycloakSettings__Issuer=https://keycloak-test-rhsso.apps.ocp4test2.mbdom.rbbh/auth/realms/rbbh`
- PROD: `KeycloakSettings__Issuer=https://keycloak-prod-rhsso.apps.ocp4prod.mbdom.rbbh/auth/realms/rbbh`

Vrijednost `KeycloakSettings__AdminClientSecret` postavlja se isključivo kroz
OCP Secret, po okruženju.

OIDC client ID i secret postoje samo na backendu kao
`KeycloakSettings__ClientId` i `KeycloakSettings__ClientSecret`; secret se
postavlja kroz OCP Secret. React ne dobija Keycloak token niti secret. Keycloak
client mora dozvoliti javni IIS URL sa `/signin-oidc` callback putanjom —
`https://rpregister.cbwebuat.mbdom.rbbh/signin-oidc` za UAT,
`https://rpregister.mbdom.rbbh/signin-oidc` za PROD.
